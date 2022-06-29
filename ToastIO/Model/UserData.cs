using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToastIO.Model
{
    /// <summary>
    /// 用户的基本资料数据
    /// </summary>
    [Serializable]
    public class UserData
    {
        public int UserID;
        public string Account, UserName, Sex;
        public UserLevel Level;
        public UserMoneyData Money;
    }
}
