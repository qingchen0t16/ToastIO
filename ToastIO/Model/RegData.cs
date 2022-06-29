using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToastIO.Model
{
    /// <summary>
    /// 注册请求数据
    /// </summary>
    [Serializable]
    public class RegData
    {
        public string NickName, Account, Password, Sex;
    }
}
