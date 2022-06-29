using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToastIO.Model
{
    /// <summary>
    /// 登录请求数据
    /// </summary>
    [Serializable]
    public class UserLogin
    {
        public string Account;
        public string Password;
    }
}
