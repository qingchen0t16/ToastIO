using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToastIO.Enum
{
    public enum SendType
    {
        Text = 0x00,    // 文本内容
        Object = 0x01,    // 对象内容
        File = 0x02,    // 文件内容
        Reply = 0x03,   // 请求返回数据
    }
}
