using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TKB995STD1553B;

namespace UFA.MILSTD1553
{
    /// <summary>
    /// Структура заголовка кадра MILSTD1553B
    /// </summary>
    public struct MILSTDCMDFrame
    {
        public short AddressOY;
        public short SubAddress;
        public short Ncd;
        public short K;

        public MILSTDCMDFrame(short addr, short subaddr, short k, short ncd)
        {
            AddressOY = addr;
            SubAddress = subaddr;
            Ncd = ncd;
            K = k;
        }
    }
    /// <summary>
    /// Класс-обертка для работы с интерфейсом MILSTD1553
    /// </summary>
    public class MILSTD1553Operation : Manchester
    {
        //private Manchester _mls;
        #region Конструкторы
        public MILSTD1553Operation() : base() { }

        #endregion

        #region Открытые методы
        /// <summary>
        /// Метод, реализующий метод базового класса в управляемом виде
        /// </summary>
        /// <param name="oldAddr">Старый адрес</param>
        /// <param name="addrOy">Текущий адрес</param>
        /// <param name="K">Направление передачи (К = 0 - передача (Ф1), К = 1 - прием (Ф2))</param>
        /// <param name="subAddr">Подадрес внутри устройства</param>
        /// <param name="ncd">Количество слов данных для передачи</param>
        /// <param name="massToSend">Одномерный массив с данными для передачи/приема</param>
        /// <returns></returns>
        public bool SendData(short oldAddr, short addrOy, short K, short subAddr, short ncd, ref ushort[] massToSend)
        {
            unsafe
            {
                fixed (ushort* ptr = &massToSend[0])
                {
                    return base.sendData(oldAddr, addrOy, K, subAddr, ncd, ptr);
                }
            }
        }

        /// <summary>
        /// Метод, реализующий метод базового класса в управляемом виде
        /// </summary>
        /// <param name="oldAddr">Старый адрес (если не нужен, то указать текущий)</param>
        /// <param name="frameStruct">Структура заголовка MILSTD</param>
        /// <param name="massToSend">Одномерный массив с данными для передачи/приема</param>
        /// <returns></returns>
        public bool SendData(short oldAddr, MILSTDCMDFrame frameStruct, ref ushort[] massToSend)
        {
            unsafe
            {
                fixed (ushort* ptr = &massToSend[0])
                {
                    return base.sendData(oldAddr, frameStruct.AddressOY, frameStruct.K, frameStruct.SubAddress, frameStruct.Ncd, ptr);
                }
            }
        }
        #endregion
    }
}
