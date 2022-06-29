using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ToastIO.Method
{
    /// <summary>
    /// 序列化
    /// </summary>
    public class Data
    {
        /// <summary>
        /// [静态] 对象变成XML String
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ObjToXml<T>(T obj)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                XmlSerializer xmlSer = new XmlSerializer(typeof(T));
                xmlSer.Serialize(stream, obj);
                stream.Position = 0;
                StreamReader sr = new StreamReader(stream);
                return Encoding.UTF8.GetBytes(sr.ReadToEnd());
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// [静态] Xml 变成 对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="strXml"></param>
        /// <returns></returns>
        public static T XmlToObj<T>(byte[] buff) where T : class
        {
            try
            {
                StringReader sr = new StringReader(Encoding.UTF8.GetString(buff));
                XmlSerializer ser = new XmlSerializer(typeof(T));
                object obj = ser.Deserialize(sr);
                sr.Close();
                return obj as T;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
