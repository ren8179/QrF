using QrF.Account.Contract;
using QrF.Framework.DAL;
using System;
using System.Collections.Generic;

namespace QrF.Account.Service
{
    public interface IAccountService
    {
        LoginInfo GetLoginInfo(Guid token);
        void UpdateLoginInfo(LoginInfo loginInfo);
        LoginInfo Login(string loginName, string password, string ip);
        void Logout(Guid token);
        void ModifyPwd(UserInput user);

        Guid SaveVerifyCode(string verifyCodeText);
        bool CheckVerifyCode(string verifyCodeText, Guid guid);

        User GetUser(int id);
        User GetUserByIp(string ip);
        IEnumerable<User> GetUserPageList(UserRequest request = null);
        /// <summary>
        /// 查找拥有某一权限的所有用户（不包括系统管理员）
        /// </summary>
        /// <param name="perssion">权限</param>
        IEnumerable<User> GetUserListByPermission(string perssion);
        void SaveUser(User user);
        void DeleteUser(List<int> ids);

        Role GetRole(int id);
        IEnumerable<Role> GetRolePageList(RoleRequest request = null);
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
        /// 查询列表(未分页)
        /// </summary>
        IEnumerable<Menu> GetMenuList(MenuRequest request = null);
        /// <summary>
        /// 查询列表(分页)
        /// </summary>
        IEnumerable<Menu> GetMenuPageList(MenuRequest request = null);
        /// <summary>
        /// 编辑保存
        /// </summary>
        void SaveMenu(Menu model);
        /// <summary>
        /// 删除
        /// </summary>
        void DeleteMenu(List<int> ids);
        /// <summary>
        /// 菜单列表转为树形结构
        /// </summary>
        AdminMenuConfig GetMenuConfig();
        /// <summary>
        /// 用户有权限访问的菜单列表
        /// </summary>
        IEnumerable<Menu> GetUserMenuList(Guid token, int parentId);
        /// <summary>
        /// 用户有权限访问的菜单列表
        /// </summary>
        AdminMenuConfig GetUserMenuList(Guid token);
        #endregion

        #region Log
        AuditLog GetAuditLog(int id);
        IEnumerable<AuditLog> GetAuditLogList(AuditLogRequest request = null);
        IEnumerable<AuditLog> GetAuditLogPageList(AuditLogRequest request = null);
        #endregion

    }
}
