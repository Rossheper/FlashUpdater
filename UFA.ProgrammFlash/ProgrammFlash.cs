using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UFA.MILSTD1553;
using UFA.IntelHexParse;
using UFA.Exceptions;
using TKB974.CRC;
using System.Threading;
using System.Threading.Tasks;

namespace UFA.ProgrammFlash
{
    /// <summary>
    /// Перечисление форматов обмена MILSTD 
    /// </summary>
    public enum FORMATS_MILSTD
    {
        F1 = 0,
        F2 = 1
    }
    /// <summary>
    /// Класс, содержащий логику программирования Изделия АЕСН.466369.001
    /// </summary>
    public class ProgrammFlash
    {
        /// <summary>
        /// Перечисление типов прошивки
        /// </summary>
        public enum TypeFRM
        {
            PLIS = 0,
            DSP = 1
        }

        /// <summary>
        /// Структура адресного пространства DPS и PLIS
        /// </summary>
        public struct startAddrPagesFRM
        {
            public ushort DSP;
            public ushort PLIS;
            public startAddrPagesFRM(ushort dsp, ushort plis)
            {
                DSP = dsp;
                PLIS = plis;
            }
        }

        /// <summary>
        /// Команды управления данными флеш памяти и прерываниями АЭ
        /// </summary>
        public enum CMD
        {
            DisableInterrupt = 1,
            EraseFlashALL = 2,
            EraseFlashSector = 3
        }

        /// <summary>
        ///  Команды для записи/чтения флеш-памяти
        /// </summary>
        protected enum FirmwareCMD
        {
            ProgrammFlash = 5,
            ReadFlash = 6
        }
        /// <summary>
        /// Структура с полями используемого адреса и подадреса
        /// </summary>
        public struct FRM_ADDR_SUB
        {
            public Int16 ADDR;
            public Int16 SUB;

            /// <summary>
            /// Конструктор инициализации текущего адреса и подадреса
            /// </summary>
            /// <param name="addr">Используемый адрес</param>
            /// <param name="sub">Используемый подадрес</param>
            public FRM_ADDR_SUB(Int16 addr, Int16 sub)
            {
                ADDR = addr;
                SUB = sub;
            }
        }

        /// <summary>
        /// Перечисление состояний перепрограммирования
        /// </summary>
        public enum PrgState
        {
            MILSTDError = 0xFF,
            Starting = 0x01,
            Processing = 0x02,
            Finished = 0x03
        }

        /// <summary>
        /// Структура, содержащая сообщение и код состояния перепрограммирования
        /// </summary>
        public struct ProgrammState
        {
            public string message;
            public PrgState state;
            public ProgrammState(PrgState st, string msg = null)
            {
                message = msg;
                state = st;
            }
        }

        /// <summary>
        /// Поле, содержащее информацию о загруженном файле/файлах
        /// </summary>
        private IntelHex _i32hex;

        /// <summary>
        /// Поле обекта, управляющего платой Elcus 
        /// </summary>
        private MILSTD1553Operation _milstd_api;
        private bool _connectIsOpen = false;
        private startAddrPagesFRM _addrPages;
        public FRM_ADDR_SUB DefaultFRMAddressing { get; set; }

        private ushort page = 0x0;
        private ushort page2 = 0xff;
        private ushort[] _getOSMilstd;

        public ProgrammFlash()
        {
            _i32hex = new IntelHex();
            _milstd_api = new MILSTD1553Operation();
            _getOSMilstd = new ushort[1];
            _connectIsOpen = ConnectionOpen() >= 0 ? true : false;
            _addrPages = new startAddrPagesFRM(0x000A, 0x0000);
            DefaultFRMAddressing = new FRM_ADDR_SUB(10, 31);


        }

        /// <summary>
        /// Преобразование строки в файла в структуру IntelHex
        /// </summary>
        /// <param name="data">Текущая строка данных IntelHex</param>
        /// <returns></returns>
        private ushort[] Data2SendPrepare(I32HEX data)
        {
            ushort[] mass2send = new ushort[Convert.ToUInt32(data.ByteCount / 2)];
            // Обработка ошибок длины массива
            if (data.ByteCount <= 0)
                throw new Exception("Ошибка! Длина данных равна нулю");
            for (int i = 0, j = 0; j < data.ByteCount; i++, j += 2)
            {
                mass2send[i] = data.Data[j + 1];
                mass2send[i] = (ushort)((mass2send[i] << 8) + data.Data[j]);
            }
            return mass2send;
        }

        /// <summary>
        /// Метод создания заголовка файла IntelHex
        /// </summary>
        /// <param name="cmd">Номер команды</param>
        /// <param name="data">Текущая структура IntelHex, содержащая строку данных</param>
        /// <returns></returns>
        private ushort[] HeaderIntelHexCreator(FirmwareCMD cmd, I32HEX data, TypeFRM typeFrm)
        {

            if ((data.Address % 0x8000) == 0)
            {
                if (typeFrm == TypeFRM.DSP)
                    page = _addrPages.DSP++;
                else
                    page = _addrPages.PLIS++;
            }

            data.Address = (ushort)((data.Address & 0x7FFF) / 2);

            //if (page2 != page)
            //    System.Diagnostics.Debug.WriteLine("Page: {0:X}", page);
            //page2 = page;
            //System.Diagnostics.Debug.WriteLine("Address: {0:X}", data.Address);
            //if (data.Address == 0x3FF8)
            //{
            //    System.Diagnostics.Debug.Write(String.Format("Data:"));
            //    for (int i = 0; i < data.ByteCount; i++)
            //    {
            //        System.Diagnostics.Debug.Write(String.Format("{0:X} ", data.Data[i]));
            //    }
            //    System.Diagnostics.Debug.WriteLine("");
            //}

            return new ushort[] { (ushort)cmd, data.Address, page };
        }

        /// <summary>
        /// Установка адресов по-умолчанию
        /// </summary>
        private void ResetAddresses()
        {
            _addrPages.PLIS = 0x0;
            _addrPages.DSP = 0x0a;
            page = 0;
        }

        /// <summary>
        /// Метод формирования команды управления состоянием
        /// </summary>
        /// <param name="cmd">Команда упралвения работы с флеш-памятью</param>
        /// <returns></returns>
        private ushort[] CreateCMD(CMD cmd)
        {
            ushort[] frameCMD = new ushort[2];
            frameCMD[0] = (ushort)cmd;
            frameCMD[1] = new CRC().CRC16PerByte(frameCMD, frameCMD.Length - 1);

            return frameCMD;
        }

        /// <summary>
        /// Метод, создающий кадр для передачи данных по MILSTD-1553B
        /// </summary>
        /// <param name="header">Массив с заголовком IntelHex</param>
        /// <param name="massData">Массив с данными IntelHex</param>
        /// <returns></returns>
        private ushort[] FrameIntelHexCreator(ushort[] header, ushort[] massData)
        {
            ushort[] frameMass = new ushort[header.Length + massData.Length + 1]; //+1 - CRC16
            Array.ConstrainedCopy(header, 0, frameMass, 0, header.Length);
            Array.ConstrainedCopy(massData, 0, frameMass, header.Length, massData.Length);
            frameMass[frameMass.Length - 1] = new CRC().CRC16PerByte(frameMass, (frameMass.Length - 1));
            return frameMass;
        }

        /// <summary>
        /// Метод, открывающий соединение с платой MILSTD-1553B
        /// </summary>
        /// <returns></returns>
        private int ConnectionOpen()
        {
            return _milstd_api.configuration(0);// _milstd_api(0);
        }

        /// <summary>
        /// Метод чтения данных из файла
        /// </summary>
        /// <param name="currFile"></param>
        private void ReadFileFRM(FileInfo currFile, TypeFRM type)
        {
            using (StreamReader reader = new StreamReader(currFile.FullName))
            {
                while (!reader.EndOfStream)
                {
                    I32HEX st = _i32hex.FRMperLine(reader.ReadLine());

                    //Retry:
                    // Если получили тип с данными!
                    if (st.RecordType == (byte)RecordType.Data)
                    {
                        /// Отправляю данные по манчестеру (Ф1)
                        // Формирую массив для отправки (заголовок + данные + CRC16)
                        ushort[] ToSend = FrameIntelHexCreator(HeaderIntelHexCreator(FirmwareCMD.ProgrammFlash, st, type), Data2SendPrepare(st));
                        ProgrammState prgState;
                        do
                        {
                            prgState = FlashCommand((CMD)FirmwareCMD.ProgrammFlash, FORMATS_MILSTD.F1, ToSend);
                            if (prgState.state == PrgState.MILSTDError)
                                Thread.Sleep(200);
                        }
                        while (prgState.state != PrgState.Finished);
                        if (prgState.state == PrgState.Finished)
                        {
                            do
                            {
                                prgState = FlashCommand((CMD)FirmwareCMD.ProgrammFlash, FORMATS_MILSTD.F2, _getOSMilstd);
                                if (prgState.state == PrgState.MILSTDError)
                                    Thread.Sleep(200);
                            }
                            while (prgState.state != PrgState.Finished);
                        }
                    }
                    // Процессинг
                    // Возможно я чтение файла выкину отсюда в основное окно, чтобы отслеживать прогерсс-баром
                }
            }
        }

        /// <summary>
        /// Метод отправки/получения данных
        /// </summary>
        /// <param name="frmaddr">Структура с адресом и подадресом</param>
        /// <param name="format">Формат передачи (к = 0, к = 1)</param>
        /// <param name="ToSend">Массив для отправки</param>
        /// <param name="msgErr">Сообщение с ошибкой</param>
        /// <returns></returns>
        private bool SendFormat(FRM_ADDR_SUB frmaddr, FORMATS_MILSTD format, ref ushort[] ToSend, out ProgrammState msgErr)
        {
            if (!_milstd_api.SendData(frmaddr.ADDR, new MILSTDCMDFrame(frmaddr.ADDR, frmaddr.SUB, (short)format, (short)(ToSend.Length)), ref ToSend))
            {
                // Ошибка
                msgErr = new ProgrammState() { message = String.Format("Количество ошибок превысило 10 при выполнении Ф {1} команды: {0}", ToSend[0], (short)format + 1), state = PrgState.MILSTDError };
                return false;
            }
            else
            {
                // Все ОК
                msgErr = new ProgrammState() { message = String.Format("Команда {0} передана успешно", ToSend[0]), state = PrgState.Finished };
                return true;
            }
        }

        #region Comments
        //private bool SendFormatF1_F2(FRM_ADDR_SUB frmaddr, ushort[] ToSend, out ProgrammState msgErr)
        //{
        //    if (!_milstd_api.SendData(frmaddr.ADDR, new MILSTDCMDFrame(frmaddr.ADDR, frmaddr.SUB, (short)FORMATS_MILSTD.F1, (short)(ToSend.Length)), ToSend))
        //    {
        //        // Ошибка в передачи (нет ОС от ОУ)
        //        msgErr = new ProgrammState() { message = "Отсутствует ответное слово от ОУ", state = PrgState.MILSTDError };
        //        return false;
        //    }
        //    else
        //    {
        //        //// Ф2 - получение кода вхождения в прерывание (А5)
        //        if (!_milstd_api.SendData(frmaddr.ADDR, new MILSTDCMDFrame(frmaddr.ADDR, frmaddr.SUB, (short)FORMATS_MILSTD.F2, (short)(ToSend.Length)), _getOSMilstd))
        //        {
        //            // Ошибка
        //            msgErr = new ProgrammState() { message = String.Format("Количество ошибок превысило 10 при выполнении Ф2 команды: {0}", ToSend[0]), state = PrgState.MILSTDError };
        //            return false;
        //        }
        //        else
        //        {
        //            // Все ОК
        //            msgErr = new ProgrammState() { message = String.Format("Команда {0} передана успешно", ToSend[0]), state = PrgState.Finished };
        //            return true;
        //        }
        //    }
        //}

        /// <summary>
        /// Метод выполняющий цикл обмена указанной команды
        /// </summary>
        /// <returns></returns>
        //virtual public ProgrammState FlashCommand(CMD cmd)
        //{

        //    if (_connectIsOpen)
        //    {
        //        ProgrammState error = new ProgrammState();
        //        ushort[] toSend = CreateCMD(cmd);
        //        ushort[] toGet = new ushort[1];
        //        _errorCountSending = 0;

        //        if (SendFormat(DefaultFRMAddressing, FORMATS_MILSTD.F1, ref toSend, out error))
        //        {

        //            do
        //            {
        //                if (false == SendFormat(DefaultFRMAddressing, FORMATS_MILSTD.F2, ref toGet, out error))
        //                    _errorCountSending++;
        //            }
        //            while ((_errorCountSending < 10) && (toSend[0] != toGet[0]));
        //        }

        //        if (_errorCountSending < 10)
        //            return new ProgrammState(PrgState.Finished);    // Отправлено удачно
        //        else
        //            return new ProgrammState(PrgState.MILSTDError, String.Format("Превышено количество попыток (> {0}) отключения прерываний",_errorCountSending));                                   // Ошибка при обмене
        //    }
        //    else
        //        return new ProgrammState(PrgState.MILSTDError, "Соединение с платой не было установлено");
        //}
        #endregion

        /// <summary>
        /// Метод управления флеш-памятью
        /// </summary>
        /// <param name="cmd">Требуемая команда</param>
        /// <param name="frmt">Формат передачи (к = 0, к = 1)</param>
        /// <param name="toSend">Массив данных для передачи (только для CMD 5 и 6)</param>
        /// <returns></returns>
        virtual public ProgrammState FlashCommand(CMD cmd, FORMATS_MILSTD frmt, ushort[] toSend = null)
        {
            if (_connectIsOpen)
            {
                ProgrammState error = new ProgrammState();
                ushort[] mass;
                if (frmt == FORMATS_MILSTD.F1)
                {
                    if ((int)cmd < (int)FirmwareCMD.ProgrammFlash)
                    {
                        mass = CreateCMD(cmd);
                    }
                    else
                    {
                        mass = toSend;
                    }
                }
                else
                    mass = new ushort[1];

                if (SendFormat(DefaultFRMAddressing, frmt, ref mass, out error))
                {
                    if (frmt == FORMATS_MILSTD.F1)
                        return new ProgrammState(PrgState.Finished);    // Отправлено удачно
                    else
                    {
                        if (mass[0] != (ushort)cmd)
                        {
                            return new ProgrammState(PrgState.MILSTDError, String.Format("Ответ от ОУ = {0}, должен соответствовать {1}", mass[0], (ushort)cmd));
                        }
                        else
                            return new ProgrammState(PrgState.Finished);    // Отправлено удачно
                    }
                }
                else
                    return error;    // Отправлено неудачно
            }
            else
                return new ProgrammState(PrgState.MILSTDError, "Соединение с платой не было установлено");
        }


        virtual protected ProgrammState FlashCommand(FirmwareCMD cmd, FORMATS_MILSTD frmt)
        {
            return FlashCommand(cmd, frmt);
        }

        /// <summary>
        /// Метод, запускающий программирование
        /// </summary>
        /// <returns></returns>
        virtual public ProgrammState StartProgramm(FileInfo FRM, TypeFRM type)
        {
            if (_connectIsOpen)
            {
                ReadFileFRM(FRM, type);
                return new ProgrammState(PrgState.Finished);
            }
            else
                return new ProgrammState(PrgState.MILSTDError, "Соединение с платой не было установлено");
        }

        /// <summary>
        /// Метод проверки целостности файла IntelHex
        /// </summary>
        virtual public void IntelHexCheckFile(FileInfo FRM)
        {
            using (StreamReader reader = new StreamReader(FRM.FullName))
            {
                try
                {
                    while (!reader.EndOfStream)
                        _i32hex.FRMperLine(reader.ReadLine());
                }
                catch (IntelHexFileCheckException err)
                {
                    err.Source = FRM.FullName;
                    throw;
                }
                catch (Exception err)
                {
                    err.Source = FRM.FullName;
                    throw;
                }
            }
        }

        virtual public void TestFile(FileInfo FRM, TypeFRM type)
        {
            ResetAddresses();
            ReadFileFRM(FRM, type);
        }
    }
}
