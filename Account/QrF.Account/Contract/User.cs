using QrF.Framework.Contract;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace QrF.Account.Contract
{
    [Auditable]
    public partial class User : ModelBase
    {
        public User()
        {
            this.Roles = new List<Role>();
            this.IsActive = true;
            this.RoleIds = new List<int>();
        }

        /// <summary>
        /// 登录名
        /// </summary>
        [Required(ErrorMessage = "登录名不能为空")]
        [StringLength(50)]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码，使用MD5加密
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [StringLength(50)]
        public string UserName { get; set; }
        public string AppId { get; set; }
        public string PartnerId { get; set; }
        /// <summary>
        /// 手机号
        /// </summary>
        [RegularExpression(@"^[1-9]{1}\d{10}$", ErrorMessage = "不是有效的手机号码")]
        [StringLength(50)]
        public string Mobile { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}", ErrorMessage = "电子邮件地址无效")]
        [StringLength(50)]
        public string Email { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [StringLength(50)]
        public string IpAddress { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// 角色列表
        /// </summary>
        public virtual List<Role> Roles { get; set; }

        [NotMapped]
        public List<int> RoleIds { get; set; }

        [NotMapped]
        public string NewPassword { get; set; }

        [NotMapped]
        public List<int> BusinessPermissionList
        {
            get
            {
                var permissions = new List<int>();

                foreach (var role in Roles)
                {
                    permissions.AddRange(role.BusinessPermissionList);
                }

                return permissions.Distinct().ToList();
            }
        }
    }
}
