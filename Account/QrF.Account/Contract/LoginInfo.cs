using QrF.Framework.Contract;
using QrF.Framework.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace QrF.Account.Contract
{
    [Serializable]
    [Table("LoginInfo")]
    public class LoginInfo : ModelBase
    {
        public LoginInfo()
        {
            LastAccessTime = DateTime.Now;
            LoginToken = Guid.NewGuid();
        }

        public LoginInfo(int userID, string loginName)
        {
            LastAccessTime = DateTime.Now;
            LoginToken = Guid.NewGuid();

            UserID = userID;
            LoginName = loginName;
        }

        public Guid LoginToken { get; set; }
        public DateTime LastAccessTime { get; set; }
        public int UserID { get; set; }
        public string LoginName { get; set; }
        public string ClientIP { get; set; }

        public int EnumLoginAccountType { get; set; }

        public string BusinessPermissionString { get; set; }

        public string ProtectedTicket { get; set; }

        [NotMapped]
        public List<int> BusinessPermissionList
        {
            get
            {
                if (string.IsNullOrEmpty(BusinessPermissionString))
                    return new List<int>();
                else
                    return BusinessPermissionString.Split(",".ToCharArray()).Select(p => int.Parse(p)).ToList();
            }
            set
            {
                BusinessPermissionString = string.Join(",", value.Select(p => (int)p));
            }
        }
    }

    [Flags]
    public enum EnumLoginAccountType
    {
        [EnumTitle("[无]", IsDisplay = false)]
        Guest = 0,
        /// <summary>
        /// 管理员
        /// </summary>
        [EnumTitle("管理员")]
        Administrator = 1,
    }
}
