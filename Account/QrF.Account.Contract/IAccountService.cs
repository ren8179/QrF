using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QrF.Account.Contract
{
    public interface IAccountService
    {
        LoginInfo GetLoginInfo(Guid token);
        LoginInfo Login(string loginName, string password);
        void Logout(Guid token);
        void ModifyPwd(User user);

        Guid SaveVerifyCode(string verifyCodeText);
        bool CheckVerifyCode(string verifyCodeText, Guid guid);

        User GetUser(int id);
        User GetUserByIp(string ip);
        IEnumerable<User> GetUserList(UserRequest request = null);
        /// <summary>
        /// 查找拥有某一权限的所有用户（不包括系统管理员）
        /// </summary>
        /// <param name="perssion">权限</param>
        IEnumerable<User> GetUserListByPermission(string perssion);
        void SaveUser(User user);
        void DeleteUser(List<int> ids);

        Role GetRole(int id);
        IEnumerable<Role> GetRoleList(RoleRequest request = null);
        void SaveRole(Role role);
        void DeleteRole(List<int> ids);

        #region Menu
        /// <summary>
        /// 查询单个对象
        /// </summary>
        Menu GetMenu(int id);
        /// <summary>
        /// 权限对应的菜单
        /// </summary>
        IEnumerable<Menu> PermissionMenu(string Permission);
        /// <summary>
        /// 查询列表(分页)
        /// </summary>
        IEnumerable<Menu> GetMenuList(MenuRequest request = null);
        /// <summary>
        /// 编辑保存
        /// </summary>
        void SaveMenu(Menu model);
        /// <summary>
        /// 删除
        /// </summary>
        void DeleteMenu(List<int> ids);
        #endregion
    }
}
