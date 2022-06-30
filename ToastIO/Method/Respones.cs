using ToastIO.Enum;
using ToastIO.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using ToastIO.API;

namespace ToastIO.Method
{
    /// <summary>
    /// 发送数据类
    /// </summary>
    public class Respones
    {
        public Dictionary<long, SourcePackage> Replys = new Dictionary<long, SourcePackage>(); // 请求返回数据
        private byte[] sourceID = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 }; // 数据发送ID
        private long SourceID
        {
            get => BitConverter.ToInt64(sourceID, 0);   // byte转换为int后输出
            set => sourceID = BitConverter.GetBytes(value);   // int转回byte 塞给sourceID
        }
        private Socket socket;

        public Respones() { }
        public Respones(Socket socket) {
            this.socket = socket;
        }

        /// <summary>
        /// 发送数据到指定Socket
        /// </summary>
        /// <param name="requestSocket"></param>
        /// <param name="sendType"></param>
        /// <param name="sendObj"></param>
        /// <param name="header"></param>
        /// <param name="sourceID"></param>
        /// <returns>返回SourceID,-1:发送失败</returns>
        public long Send<T>(Socket requestSocket, SendType sendType, T sendObj, string header = "Null", long sendSourceID = -1, Action<SourcePackage> endReceive = null)
        {
            if (!requestSocket.Connected)   // Socket已经关闭
                throw new Exception("Socket已经关闭");

            byte[] headByte = new byte[30],
                   headByteOld = Encoding.UTF8.GetBytes(header);
            Array.Copy(headByteOld, 0, headByte, 0, headByteOld.Length);    // 需要把header转成30长度的byte数组

            // 循环发送数据
            foreach (byte[] item in GetSendBuff(Data.ObjToXml(sendObj),
                                                sendSourceID == -1 ? sourceID : BitConverter.GetBytes(sendSourceID),
                                                headByte,
                                                sendType))
                requestSocket.Send(item);

            if(endReceive != null)  // 有回调函数
                Replys.Add(sendSourceID == -1 ? SourceID : sendSourceID, new SourcePackage(requestSocket, endReceive));
            
            return sendSourceID == -1 ? SourceID++ : sendSourceID;  // 返回SourceID,如果sourceID是-1就使用当前Respones的sourceID
        }

        /// <summary>
        /// 基于FSRequest的发送数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sendType"></param>
        /// <param name="sendObj"></param>
        /// <param name="header"></param>
        /// <param name="sendSourceID"></param>
        /// <param name="endReceive"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public long Send<T>(SendType sendType, T sendObj, string header = "Null", long sendSourceID = -1, Action<SourcePackage> endReceive = null) {
            if(socket is null)
                throw new Exception("无法发送数据，因为Socket是空的");
            return Send<T>(socket,sendType, sendObj, header, sendSourceID, endReceive);
        }

        /// <summary>
        /// 批量发送数据到Socket
        /// </summary>
        /// <param name="requestSockets"></param>
        /// <param name="sendType"></param>
        /// <param name="sendObj"></param>
        /// <param name="header"></param>
        /// <param name="sourceID"></param>
        /// <returns>返回发送成功个数</returns>
        public int BatchSend<T>(Socket[] requestSockets, SendType sendType, T sendObj, string header = "Reply", long sourceID = -1, long sendSourceID = -1, Action<SourcePackage> endReceive = null)
        {

            int sendCount = 0;
            foreach (Socket socket in requestSockets)
                sendCount += Send(socket, sendType, sendObj, header, sendSourceID, endReceive) == -1 ? 0 : 1;
            return sendCount;
        }

        /// <summary>
        /// 分割成分批发送的数据列表
        /// </summary>
        /// <param name="buff">数据</param>
        /// <param name="sendSourceID">数据ID</param>
        /// <param name="header">包头</param>
        /// <param name="sendType">包类型</param>
        /// <param name="pageLen">包长 def: 1024 包长不能小于100</param>
        /// <returns>分批分割的数据列表</returns>
        public List<byte[]> GetSendBuff(byte[] buff, byte[] sendSourceID, byte[] header, SendType sendType, int pageLen = FSRequest.MEMORYPOOL)
        {
            /**
             *  包结构 byte[]
                 *  0-7      SourceID
                 *  8        包类型
                 *  9-38     包头
                 *  39-42    包总数
                 *  43-46    包索引
                 *  47-50    数据报长
                 *  51-        包数据
             */

            // 抛出异常
            if (pageLen < 100)   // 自定包长小于100 (发送数据过于缓慢,丢弃)
                throw new ArgumentOutOfRangeException("GetSendBuff", "自定义包长必须大于100");
            if (header.Length != 30)    // 包头长度不符合标准
                throw new ArgumentOutOfRangeException("GetSendBuff", "包头长度不符合标准(必须小于等于30)");

            int pageDataLen = (pageLen - 53);   // 实际可传输数据包长
            List<byte[]> sendBuff = new List<byte[]>(); // 总包列表
            List<byte> sendBuffHeader = new List<byte>();   // 包头
            sendBuffHeader.AddRange(sendSourceID);     // 把SourceID塞进去
            sendBuffHeader.Add((byte)sendType);    // sendType
            sendBuffHeader.AddRange(header);    // 加入包Head
            int sendCount = buff.Length % pageDataLen == 0 ? buff.Length / pageDataLen : buff.Length / pageDataLen + 1; // 总发包数
            sendBuffHeader.AddRange(BitConverter.GetBytes(sendCount)); // 塞进去包总数

            // 循环加入包
            for (int i = 0; i < sendCount; i++)
            {
                List<byte> tempList = new List<byte>(); // 临时的数据包
                /*
                    设置包长
                    如果剩余数据长度 < 默认数据包长
                    数据包长则等于剩余数据长度
                    否则就是默认的数据包长
                 */
                pageDataLen = (buff.Length < (i + 1) * pageDataLen ? buff.Length - i * pageDataLen : pageLen - 53);
                tempList.AddRange(sendBuffHeader.ToArray());    // 塞入头
                tempList.AddRange(BitConverter.GetBytes(i));    // 塞入第几个包
                tempList.AddRange(BitConverter.GetBytes(pageDataLen));    // 塞入报长
                tempList.AddRange(buff.Skip(i * (pageLen - 53)).Take(pageDataLen).ToArray());   // 获取当前字段byte内容 塞入buff列表
                sendBuff.Add(tempList.ToArray());    // 塞入数据
            }

            return sendBuff;
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        public void Beat(Socket requestSocket) {
            if (!requestSocket.Connected)   // Socket已经关闭
                throw new Exception("Socket已经关闭");
            requestSocket.Send(new byte[] {1});
        }
    }
}
