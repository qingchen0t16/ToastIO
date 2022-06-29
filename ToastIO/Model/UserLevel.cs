using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToastIO.Model
{
    /// <summary>
    /// 用户等级
    /// </summary>
    [Serializable]
    public class UserLevel
    {
        public int Level;
        public long Exp;
        public long NeedExp;
    }
}
