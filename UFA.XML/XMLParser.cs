using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Xml.Linq;
using System.Text.RegularExpressions;
namespace UFA.XML
{
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
    public class XMLParser
    {
        public static string xmlFile = "Settings.xml";
        private XDocument _rootdoc;
        public XMLParser()
        {
            _rootdoc = XDocument.Load(xmlFile);
        }
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
        public ADDR_SUB GetAddrSub
        {
            get
            {
                var addresses = from milstd in _rootdoc.Root.Descendants("MILSTD1553B") where milstd.Attribute("setting").Value.Contains("programming") select milstd;
                ADDR_SUB addrSubLoad = new ADDR_SUB();
                foreach (var c in addresses.Nodes())
                {
                    string data = Regex.Match(((XElement)c).Value, @"\d+").Value;
                    switch (((XElement)c).Name.LocalName)
                    {
                        case "addr":
                            {
                                addrSubLoad.addr = data == null ? 0 : Convert.ToInt32(data);
                                break;
                            }
                        case "subaddr":
                            {
                                addrSubLoad.sub = data == null ? 0 : Convert.ToInt32(data);
                                break;
                            }
                    }
                }
                return addrSubLoad;
            }
        }
        public int GetDspStartPage
        {
            get
            {
                var page = from milstd in _rootdoc.Root.Descendants("DSP") select milstd;
                foreach (var c3 in page.Nodes())
                {
                    if (((XElement)c3).Name.LocalName.Contains("StartPage"))
                    {
                        string data = Regex.Match(((XElement)c3).Value, @"\d+").Value;
                        return data == null ? 0 : Convert.ToInt32(data);
                    }
                }
                return -1;
            }
        }
        public int GetPLISStartPage
        {
            get
            {
                var page = from milstd in _rootdoc.Root.Descendants("PLIS") select milstd;
                foreach (var c3 in page.Nodes())
                {
                    if (((XElement)c3).Name.LocalName.Contains("StartPage"))
                    {
                        string data = Regex.Match(((XElement)c3).Value, @"\d+").Value;
                        return data == null ? 0 : Convert.ToInt32(data);
                    }
                }
                return -1;
            }
        }
    }
}
