using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace Abp.Xml
{
    /// <summary>
    /// Defines helper methods to work with XML.
    /// </summary>
    public static class XmlSerializationHelper
    {
        /// <summary>
        /// Load From XML
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T LoadFromXml<T>(string filePath)
        {
            FileStream fs = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return (T)serializer.Deserialize(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Serializes an object with a type information included.
        /// So, it can be deserialized using <see cref="XmlDeserialize"/> method later.
        /// </summary>
        public static string XmlSerialize(object serialObject, bool removeDataRootXmlNode = false)
        {
            StringBuilder sb = new StringBuilder();
            XmlSerializer ser = new XmlSerializer(serialObject.GetType());
            using (TextWriter writer = new StringWriter(sb))
            {
                ser.Serialize(writer, serialObject);
                string xmlData = writer.ToString();
                if (removeDataRootXmlNode)
                {
                    System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                    doc.LoadXml(xmlData);
                    xmlData = doc.LastChild.InnerXml;
                }
                return xmlData;
            }
        }

        /// <summary>
        /// Deserialize string information included.
        /// So, it can be Serializes using <see cref="XmlSerialize"/> method later.
        /// </summary>
        public static object XmlDeserialize(string str, Type type)
        {
            XmlSerializer mySerializer = new XmlSerializer(type);
            using (TextReader reader = new StringReader(str))
            {
                return mySerializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Deserialize string information included.
        /// So, it can be Serializes using <see cref="XmlSerialize"/> method later.
        /// </summary>
        public static T XmlDeserialize<T>(string str)
        {
            return (T)XmlDeserialize(str, typeof(T));
        }
    }
}
