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
        private UInt32 _errorCountSending = 0;

        public enum TypeFRM
        {
            PLIS = 0,
            DSP = 1
        }
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
        public enum CMD
        {
            DisableInterrupt = 1,
            EraseFlashALL = 2,
            EraseFlashSector = 3,
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
        private MILSTD1553Operation _milstd_api;
        private bool _connectIsOpen = false;
        private startAddrPagesFRM _addrPages;
        public FRM_ADDR_SUB DefaultFRMAddressing { get; set; }

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
        private ushort[] HeaderIntelHexCreator(CMD cmd, I32HEX data, TypeFRM typeFrm)
        {
            ushort page = 0x0;
            if (typeFrm == TypeFRM.DSP)
            {
                if ((data.Address % 0x8000) == 0)
                    page = _addrPages.DSP++;
                else
                    page = _addrPages.DSP;
            }
            else
            {
                if ((data.Address % 0x4000) == 0)
                    page = _addrPages.PLIS++;
                else
                    page = _addrPages.PLIS;
            }

            data.Address = (ushort)((data.Address & 0x7FFF) / 2);
            return new ushort[] { (ushort)cmd, data.Address, page };
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

                    Retry:
                    // Если получили тип с данными!
                    if (st.RecordType == (byte)RecordType.Data)
                    {
                        /// Отправляю данные по манчестеру (Ф1)
                        // Формирую массив для отправки (заголовок + данные + CRC16)
                        ushort[] ToSend = FrameIntelHexCreator(HeaderIntelHexCreator(CMD.ProgrammFlash, st, type), Data2SendPrepare(st));
                        if (!_milstd_api.SendData(10, new MILSTDCMDFrame(10, 21, 0, (short)(ToSend.Length)), ref ToSend))
                        {
                            goto Retry;
                            // Все неудачно, повторить посылку
                        }
                        else
                        {

                        }
                        //// Ф2 - получение кода вхождения в прерывание (А5)
                        _milstd_api.SendData(10, new MILSTDCMDFrame(10, 21, 0, (short)(_getOSMilstd.Length)), ref _getOSMilstd);

                        if (_getOSMilstd[0] == 0xA500)
                        {

                            // В прерывание вошли
                            _milstd_api.SendData(10, 10, 1, 21, 1, ref _getOSMilstd);
                            //Проверка записи данных во флеш!
                            //// Ф2 - получение команды и статуса Флешки (1 - не записано, 0 - все ОК)
                            if (((_getOSMilstd[0] & 0xFF00) >> 8) == 0)
                            {
                                // Записано успешно, продолжаем
                                continue;
                            }
                            else
                            {
                                goto Retry;
                                // Ошибка записи во флеш - повторить запись тех же данных
                            }
                        }
                        else
                        {
                            goto Retry;
                        }
                    }
                }
            }
        }

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

        virtual public ProgrammState FlashCommand(CMD cmd, FORMATS_MILSTD frmt, ref uint errors)
        {

            if (_connectIsOpen)
            {
                ProgrammState error = new ProgrammState();
                ushort[] toSend = CreateCMD(cmd);
                ushort[] toGet = new ushort[1];
                _errorCountSending = 0;

                if (SendFormat(DefaultFRMAddressing, frmt, ref toSend, out error))
                {
                    errors = 0;
                    return new ProgrammState(PrgState.Finished);    // Отправлено удачно
                }
                else
                {
                    errors++;
                    return new ProgrammState(PrgState.MILSTDError,"Отправка неуспешна");    // Отправлено неудачно
                }
            }
            else
                return new ProgrammState(PrgState.MILSTDError, "Соединение с платой не было установлено");
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
                return new ProgrammState();
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
    }
}
