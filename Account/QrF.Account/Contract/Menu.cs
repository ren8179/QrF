using QrF.Framework.Contract;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QrF.Account.Contract
{
    [Table("Menu")]
    public class Menu : ModelBase
    {
        public Menu()
        {
            this.ParentId = 0;
        }
        /// <summary>
        /// 菜单名称
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// 页面地址
        /// </summary>
        public String Url { get; set; }
        /// <summary>
        /// 描述信息
        /// </summary>
        public String Info { get; set; }
        /// <summary>
        /// 菜单编号
        /// </summary>
        public String Code { get; set; }
        /// <summary>
        /// 认证编号
        /// </summary>
        public String Permission { get; set; }
        /// <summary>
        /// 菜单图标
        /// </summary>
        public String Icon { get; set; }
        /// <summary>
        /// 父级菜单
        /// </summary>
        public Int32? ParentId { get; set; }
        public Menu Parent { get; set; }
        /// <summary>
        /// 排列顺序
        /// </summary>
        public string Orderby { get; set; }
    }
}
