using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToastIO.Model
{
    /// <summary>
    /// 发送信息数据
    /// </summary>
    [Serializable]
    public class SendMessage
    {
        public string SendType; // 公众，私聊，公会
        public int Id;
        public string Message;
    }
}
