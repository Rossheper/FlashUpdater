using System;
using System.Runtime.InteropServices;
using UFA.Exceptions;
namespace UFA.IntelHexParse
{
    public enum RecordType
    {
        Data = 0x00,
        EOF = 0x01,
        ExtendedSegmentAddrees = 0x02,
        StartSegmentAddress = 0x03,
        ExtendedLinearAddress = 0x04,
        StartLinearAddress = 0x05
    }
    /// <summary>
    /// Структура заголовка IntelHex (I32HEX)
    /// </summary>
    public struct I32HEX
    {
        #region Поля
        public byte ByteCount;
        public UInt16 Address;
        public byte RecordType;
        public byte[] Data;
        public Byte Checksum;
        public Byte Checksum_file;
        #endregion
        #region Конструкторы
        /// <summary>
        /// Конструктор инициализации заголовка I32HEX
        /// </summary>
        /// <param name="_lenheader">Длина байт заголовка</param>
        public I32HEX(out UInt16 _lenheader)
        {
            ByteCount = 0;
            Address = 0;
            RecordType = 0;
            Data = null;
            Checksum = 0;
            Checksum_file = 0;

            _lenheader = Convert.ToUInt16(Marshal.SizeOf(ByteCount) + Marshal.SizeOf(Address) + Marshal.SizeOf(RecordType));
        }
        #endregion
    }
    /// <summary>
    /// Класс для обработки и структурирования файла IntelHex
    /// </summary>
    public class IntelHex
    {
        #region Поля
        private UInt16 _lenheader = 4; // Длина заголовка I32HEX в байтах
        private const UInt16 _offset = 2; // Для преобразования символов строки в байты
        // Структура сообщения
        private I32HEX _hexLine;
        #endregion

        #region Конструкторы
        /// <summary>
        /// Конструктор, инициализирующий заголовок формата IntelHex
        /// </summary>
        public IntelHex()
        {
            _hexLine = new I32HEX(out _lenheader);
        }
        #endregion
        #region Открытые методы
        /// <summary>
        /// Формирование структуры I32HEX для текущей строки файла IntelHex
        /// </summary>
        /// <param name="line">Текущая строка из файла IntelHex (вклчюая ":")</param>
        /// <returns></returns>
        public I32HEX FRMperLine(string line)
        {
            if (line == null || line[0] != ':' || ((line.Length % 2) == 0))
                throw new IntelHexFileCheckException("Неверный формат файла с прошивкой IntelHex");
            else
                line = line.TrimStart(new char[] { ':' });
            return Line2IntelHex(line);

        }
        #endregion
        #region Закрытые методы
        /// <summary>
        /// Преобразование каждой строки файла типа IntelHex в структуру I32HEX
        /// </summary>
        /// <param name="line">Строка файла IntelHex</param>
        /// <returns>Структурированная строка файла IntelHex</returns>
        private I32HEX Line2IntelHex(string line)
        {
            _hexLine = new I32HEX();
            for (int i = 0, j = 0; i < line.Length; i += _offset, j++)
            {
                byte data = (byte)((Convert.ToInt32(line[i].ToString(), 16) << 4) + Convert.ToInt32(line[i + 1].ToString(), 16));
                // Заголовок и данные
                if (i < line.Length - _offset)
                {
                    if (j < _lenheader)
                        GetIntelHeader(j, data);
                    else
                        _hexLine.Data[j - (_lenheader)] = data;

                    _hexLine.Checksum += data;
                }
                else // Checksum из файла
                    _hexLine.Checksum_file = data;
            }

            _hexLine.Checksum = (byte)((~_hexLine.Checksum & 0xff) + 1);

            if (_hexLine.Checksum != _hexLine.Checksum_file)
                throw new IntelHexFileCheckException(String.Format("Ошибка при расчете Checksum в строке \n :{0}", line));

            return _hexLine;
        }
        /// <summary>
        /// Формирование заголовка файла IntelHEX
        /// </summary>
        /// <param name="j">Номер байта</param>
        /// <param name="data">Значение байта</param>
        private void GetIntelHeader(int j, byte data)
        {
            switch (j)
            {
                case 0: { _hexLine.ByteCount = data; _hexLine.Data = new byte[_hexLine.ByteCount]; break; }
                case 1: { _hexLine.Address = data; break; }
                case 2: { _hexLine.Address = (UInt16)((_hexLine.Address << 8) + data); break; }
                case 3: { _hexLine.RecordType = data; break; }
            }
        }
        #endregion
    }
}
