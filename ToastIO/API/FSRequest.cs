using ToastIO.Enum;
using ToastIO.Method;
using ToastIO.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ToastIO.API
{
    public class FSRequest
    {
        //常量
        public const int MEMORYPOOL = 1024 * 1024 * 3;  // 内存池大小
        // 属性
        public byte[] buff = new byte[MEMORYPOOL];    // 请求单次传入数据
        private Socket request; // 请求的Socket 可能是客户端 也可能是服务端
        public Socket Request { 
            get => request;
            set {
                request = value;
                Respones = new Respones(request);
            }
        }
        public string IP { get => ((IPEndPoint)Request.RemoteEndPoint).Address.ToString(); }    // 请求Socket的IP
        public string Port { get => ((IPEndPoint)Request.RemoteEndPoint).Port.ToString(); }     // 请求Socket的端口
        private List<byte[]> buffCookie = new List<byte[]>();   // 防止数据连接 粘包
        protected Dictionary<long, SourcePackage> pages = new Dictionary<long, SourcePackage>();   // 接收的包体(根据SourceID)
        public Respones Respones;
        public DateTime LastBeatTime;   // 最后一次Beat时间 
        public bool IsOpen { get; private set; } = false;   // 是否开启
        // 委托
        public Action<FSRequest, Exception> ConnectionFailedListener;  // 绑定服务器失败
        public Action<FSRequest, IAsyncResult> SocketEnterListener;    // 请求进入
        public Action<FSRequest, Exception> DataEnterExListener;   // SocketEnter 异常处理
        public Action<FSRequest, byte[]> PagePushFailedListener;   // 无法解析数据
        public Action<FSRequest, string> RequestCloseListener;    // 请求的Socket关闭
        public Action<SourcePackage> EndReceiveListener;    // 包体接收完毕处理

        /// <summary>
        /// 绑定服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Bind(string ip = "127.0.0.1", int port = 25565)
        {
            Request = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  // 初始化Socket
            try
            {
                // ClientConnect(new IPEndPoint(IPAddress.Parse(ip), port));   // 连接服务器
                Request.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                Request.BeginReceive(buff, 0, 1024, SocketFlags.None, new AsyncCallback(DataEnter), Request);  // 处理请求
                // 开始心跳
                new Thread(Beat).Start();  
                DoBeat();
                IsOpen = true;
            }
            catch (Exception ex)
            {
                if (ConnectionFailedListener != null)
                    ConnectionFailedListener(this, ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 心跳
        /// </summary>
        public void Beat()
        {
            while (true)
            {
                try
                {
                    System.Threading.Thread.Sleep(1000);    // 心跳频率
                    Respones.Beat(Request);
                    TimeSpan sp = System.DateTime.Now - LastBeatTime;
                    if (sp.Seconds >= 3)    // 心跳时间大于三秒 
                    {
                        if (RequestCloseListener != null)
                            RequestCloseListener(this, "未知原因断开的连接");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (RequestCloseListener != null)
                        RequestCloseListener(this, ex.Message);
                    return;
                }
            }
        }

        /// <summary>
        /// 处理心跳
        /// </summary>
        public void DoBeat()
        {
            LastBeatTime = DateTime.Now;    // 更新时间
        }

        /// <summary>
        /// 启动数据监听
        /// </summary>
        public void StartListenerData()
            => Request.BeginReceive(buff, 0, 1024, SocketFlags.None, new AsyncCallback(DataEnter), Request);  // 处理请求

        /// <summary>
        /// 启动客户进入监听
        /// </summary>
        /// <param name="ar"></param>
        public void StartListen()
            => Request.BeginAccept(new AsyncCallback(ClientAccepted), Request);
        public void ClientAccepted(IAsyncResult ar)
        {
            Socket socket = ar.AsyncState as Socket,    // 拿回对方socket
                   client = socket.EndAccept(ar);       // 拿回自身socket
            if (SocketEnterListener != null)
                SocketEnterListener(this, ar);
            socket.BeginAccept(new AsyncCallback(ClientAccepted), socket);  // 等待下一个Client
        }

        /// <summary>
        /// 数据进入
        /// </summary>
        /// <param name="ar"></param>
        public void DataEnter(IAsyncResult ar)
        {
            if (!Request.Connected)
            {
                if (RequestCloseListener != null)
                    RequestCloseListener(this, "未知原因断开的连接");
                return;
            }
            try
            {
                var pageLen = Request.EndReceive(ar);    // 获取数据长度
                if (pageLen == 0)    // 如果数据长度为0 掉线
                {
                    if (RequestCloseListener != null)
                        RequestCloseListener(this, "网络原因(掉线)断开的连接");
                    return;
                }
                if (pageLen == 1)    // 如果数据长度为1 心跳
                {
                    DoBeat();
                    Request.BeginReceive(buff, 0, MEMORYPOOL, SocketFlags.None, new AsyncCallback(DataEnter), Request); // 监听并处理新的Request请求
                    return;
                }
                long sourceID = SourcePackage.GetSourceID(buff);    // 取出sourceID
                buffCookie.Add(buff.Take(pageLen).ToArray());
                int index = buffCookie.Count - 1;
                if (SourcePackage.GetSendType(buff) != SendType.Reply)
                {
                    // 没接受过这个SourceID的包
                    if (!pages.ContainsKey(sourceID))
                        pages.Add(sourceID, new SourcePackage(Request, EndReceiveListener));

                    if (!pages[sourceID].Push(buffCookie[index]))  // 错误的数据
                        if (PagePushFailedListener != null)
                            PagePushFailedListener(this, buffCookie[index]);
                }
                else if (Respones.Replys.ContainsKey(sourceID))
                {
                    if (!Respones.Replys[sourceID].Push(buffCookie[index]))  // 错误的数据
                        if (PagePushFailedListener != null)
                            PagePushFailedListener(this, buffCookie[index]);
                }
                buffCookie.RemoveAt(index); // 清除Cookie

                buff = new byte[MEMORYPOOL];
                Request.BeginReceive(buff, 0, MEMORYPOOL, SocketFlags.None, new AsyncCallback(DataEnter), Request); // 监听并处理新的Request请求
            }
            catch (Exception ex)
            {
                // 抛出异常
                if (DataEnterExListener != null)
                    DataEnterExListener(this, ex);
            }
        }
    }

}
