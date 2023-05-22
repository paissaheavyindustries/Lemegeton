using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Lemegeton.Core
{

    static class XmlSerializer<T>
    {

        public static T Deserialize(string input)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            byte[] buf = UTF8Encoding.UTF8.GetBytes(input);
            using (MemoryStream ms = new MemoryStream(buf))
            {
                return (T)xs.Deserialize(ms);
            }
        }

        public static T Deserialize(XmlDocument doc)
        {
            return Deserialize(doc.OuterXml);
        }

        public static string Serialize(T obj)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                xs.Serialize(ms, obj, ns);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }

    }

}
