using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TKB995STD1553B;
namespace UFA.MILSTD1553
{
    /// <summary>
    /// Класс-обертка для работы с интерфейсом MILSTD1553
    /// </summary>
    public class MILSTD1553Operation : Manchester
    {
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
        public bool SendData(short oldAddr, short addrOy, short K, short subAddr, short ncd, ushort[] massToSend)
        {
            unsafe
            {
                fixed (ushort* ptr = &massToSend[0])
                {
                    return base.sendData(oldAddr, addrOy, K, subAddr, ncd, ptr);
                }
            }
        }
        #endregion
    }
}
