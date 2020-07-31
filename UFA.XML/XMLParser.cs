using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using UFA.Exceptions;

namespace UFA.XML
{
    /// <summary>
    /// Структура хранящая адрес и подадрес ОУ
    /// </summary>
    public struct ADDR_SUB
    {
        public int addr;
        public int sub;

        public ADDR_SUB(int a, int s)
        {
            addr = a;
            sub = s;
        }
    }

    /// <summary>
    /// Класс для разбора файла с настройками
    /// </summary>
    public class XMLSettingsParser
    {
        private string _regHex = @"[^0x]\w*";                   // шаблон для чтения HEX числа из строки формата (0x....)
        private string _regDigit = @"\d+";                      // шаблон для чтения любой цифры из файла
        private XDocument _rootdoc;                             // объект, содержащий информацию файла (Settings.xml) с настройками
        public static string xmlFile = "Settings.xml";          // файл с текущими настройками

        public XMLSettingsParser()
        {
            try
            {
                _rootdoc = XDocument.Load(xmlFile);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Читает XML-файл Settings.xml с настройками и возвращает номер платы MILSTD-1553B
        /// </summary>
        public int GetNumberPlate
        {
            get
            {
                int plate_to_config = 0;
                var plate = from milstd in _rootdoc.Root.Descendants("MILSTD1553B") where milstd.Attribute("setting").Value.Contains("configuration") select milstd.Value;
                string plateNum = plate.FirstOrDefault();
                if (plateNum == null)
                    return 0;
                else
                {
                    Int32.TryParse(plateNum, out plate_to_config);
                    return plate_to_config;
                }
            }
        }

        /// <summary>
        /// Читает XML-файл Settings.xml с настройками и возвращает структуру с адресом и подадресом оконечного устройства (ОУ)
        /// </summary>
        public ADDR_SUB GetAddrSub
        {
            get
            {
                var addresses = from milstd in _rootdoc.Root.Descendants("MILSTD1553B") where milstd.Attribute("setting").Value.Contains("programming") select milstd;
                ADDR_SUB addrSubLoad = new ADDR_SUB();
                foreach (var c in addresses.Nodes())
                {
                    string data = Regex.Match(((XElement)c).Value, _regDigit).Value;
                    switch (((XElement)c).Name.LocalName)
                    {
                        case "addr":
                            {
                                addrSubLoad.addr = data == null ? 10 : Convert.ToInt32(data);
                                break;
                            }
                        case "subaddr":
                            {
                                addrSubLoad.sub = data == null ? 30 : Convert.ToInt32(data);
                                break;
                            }
                    }
                }
                return addrSubLoad;
            }
        }

        /// <summary>
        /// Читает XML-файл Settings.xml с настройками и возвращает стартовый адрес страницы внутри флеш-памяти прошивки DSP
        /// </summary>
        public int GetDspStartPage
        {
            get
            {
                var page = from milstd in _rootdoc.Root.Descendants("DSP") select milstd;
                foreach (var startPage in page.Nodes())
                {
                    if (((XElement)startPage).Name.LocalName.Contains("StartPage"))
                    {
                        // Элемент StartPage найден, читаю значение
                        
                        return StringHexToInt(((XElement)startPage).Value);
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// Читает XML-файл Settings.xml с настройками и возвращает стартовый адрес страницы внутри флеш-памяти прошивки PLIS
        /// </summary>
        public int GetPLISStartPage
        {
            get
            {
                // Вытаскиваю все элементы с тегом PLIS
                var page = from milstd in _rootdoc.Root.Descendants("PLIS") select milstd;
                foreach (var startPage in page.Nodes())
                {
                    if (((XElement)startPage).Name.LocalName.Contains("StartPage"))
                    {
                        // Элемент StartPage найден, читаю значение
                        return StringHexToInt(((XElement)startPage).Value);
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// Преобразование строки с числом HEX в int
        /// </summary>
        /// <param name="startPage">Строка с числом в HEX формате</param>
        /// <returns></returns>
        private int StringHexToInt(string startPage)
        {
            Int32 hexPage = 0;
            if (Int32.TryParse(Regex.Match(startPage, _regHex).Value, System.Globalization.NumberStyles.HexNumber, new System.Globalization.CultureInfo("en-US"), out hexPage))
                return hexPage;
            else
                return 0;
        }
    }
}
