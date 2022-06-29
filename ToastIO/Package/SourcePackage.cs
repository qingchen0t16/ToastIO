using ToastIO.Enum;
using ToastIO.Method;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ToastIO.Package
{
    public class SourcePackage
    {
        // 包参数
        public long SourceID { get; set; }  // 资源ID
        public int PageCount; // 总包数
        private int pageReceiveNum;
        public int PageReceiveNum
        {
            get => pageReceiveNum; set
            {
                pageReceiveNum = value;
                if (pageReceiveNum == PageCount)    // 包数 = 总包数 启动接收完毕的线程
                    new Thread((sp) => EndReceive((SourcePackage)sp)).Start(this);  // 数据接收完成执行
            }
        }   //已接受包数
        private List<byte> data = new List<byte>();    // 接收到的数据
        public SendType SendType;   // 包类型
        public string Header;  // 包头
        public Socket RequestSocket;   // 请求的Socket 可能是客户端 也可能是服务端
        public Respones Respones = new Respones();

        // 其他参数
        private List<int> pageIndexRecord = new List<int>();    // 用来排序发来的数据
        public Action<SourcePackage> EndReceive;    // 数据接收完毕的委托

        public SourcePackage(Socket requestSocket, Action<SourcePackage> endReceive)
        {
            SourceID = -1;
            RequestSocket = requestSocket;
            EndReceive = endReceive;
        }

        /// <summary>
        /// 回复发送者数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sendObj"></param>
        /// <param name="header"></param>
        /// <param name="endReceive"></param>
        public void Reply<T>(T sendObj, string header = "Null", Action<SourcePackage> endReceive = null) => Respones.Send(RequestSocket, SendType.Reply, sendObj, header, SourceID, endReceive);

        /// <summary>
        /// 数据提交
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        public bool Push(byte[] buff)
        {
            if (buff.Length < 51)   // 错误的包体
                throw new ArgumentException("SourcePackage","错误的包体");

            if (SourceID == -1)
            {
                // 第一次Push
                SourceID = GetSourceID(buff);   // 获取SourceID
                SendType = (SendType)buff[8];   // 获取数据类型
                Header = GetPageHeader(buff);   // 获取包头
                PageCount = BitConverter.ToInt32(new byte[] { buff[39], buff[40], buff[41], buff[42] }, 0);
            }
            // 获取包索引
            int index = BitConverter.ToInt32(new byte[] { buff[43], buff[44], buff[45], buff[46] }, 0);
            int pageLen = BitConverter.ToInt32(new byte[] { buff[47], buff[48], buff[49], buff[50] }, 0);  // 数据长
            byte[] buf = buff.Skip(51).Take(pageLen).ToArray(); // 数据存放

            if (pageIndexRecord.Count == 0) // 如果是第一次
            {
                data.AddRange(buf); // 加入数据
                pageIndexRecord.Add(index); // 加入包索引
                PageReceiveNum++;
                return true;
            }
            for (int i = 0; i < pageIndexRecord.Count; i++)
                if (index < pageIndexRecord[i])
                {
                    pageIndexRecord.Insert(i, index);
                    data.InsertRange(i * 974, buf); //数据插入到指定位置
                    PageReceiveNum++;
                    return true;
                }
                else if (index > pageIndexRecord[i])
                {
                    if (i == pageIndexRecord.Count - 1) // 最后一个
                    {
                        pageIndexRecord.Add(index); // 在最后插入
                        data.AddRange(buf); //数据插入i * 240的位置
                        PageReceiveNum++;
                        return true;
                    }
                }
            return false;
        }

        /// <summary>
        /// 获取当前包的数据
        /// </summary>
        public T GetSource<T>() where T : class => Data.XmlToObj<T>(data.ToArray());

        /// <summary>
        /// 静态方法 获取包头
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        public static string GetPageHeader(byte[] buff)
        {
            List<byte> header = new List<byte>();
            for (int i = 0; i < 30; i++)
                if (buff[9 + i] != 0x00)
                    header.Add(buff[9 + i]);
                else
                    break;
            return Encoding.UTF8.GetString(header.ToArray());
        }

        /// <summary>
        /// 静态方法 获取SourceID
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        public static long GetSourceID(byte[] buff) => BitConverter.ToInt64(new byte[] { buff[0], buff[1], buff[2], buff[3], buff[4], buff[5], buff[6], buff[7] }, 0);

        /// <summary>
        /// 静态方法 获取SendType
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        public static SendType GetSendType(byte[] buff) => (SendType)buff[8];
    }
}
