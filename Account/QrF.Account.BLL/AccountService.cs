using EntityFramework.Extensions;
using QrF.Account.Contract;
using QrF.Account.DAL;
using QrF.Core.Cache;
using QrF.Core.Helper;
using QrF.Framework.Contract;
using QrF.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QrF.Account.BLL
{
    public class AccountService : IAccountService
    {
        private readonly int _UserLoginTimeoutMinutes = 60;
        private readonly string _LoginInfoKeyFormat = "LoginInfo_{0}";

        #region LoginInfo
        public LoginInfo GetLoginInfo(Guid token)
        {
            return CacheHelper.Get<LoginInfo>(string.Format(_LoginInfoKeyFormat, token), () =>
            {
                using (var dbContext = new AccountDbContext())
                {
                    //如果有超时的，启动超时处理
                    var timeoutList = dbContext.FindAll<LoginInfo>(p => DbFunctions.DiffMinutes(DateTime.Now, p.LastAccessTime) > _UserLoginTimeoutMinutes);
                    if (timeoutList.Count > 0)
                    {
                        foreach (var li in timeoutList)
                            dbContext.LoginInfos.Remove(li);
                    }

                    dbContext.SaveChanges();


                    var loginInfo = dbContext.FindAll<LoginInfo>(l => l.LoginToken == token).FirstOrDefault();
                    if (loginInfo != null)
                    {
                        loginInfo.LastAccessTime = DateTime.Now;
                        dbContext.Update<LoginInfo>(loginInfo);
                    }

                    return loginInfo;
                }
            });
        }

        public LoginInfo Login(string loginName, string password)
        {
            LoginInfo loginInfo = null;
            password = Encrypt.MD5(password);
            loginName = loginName.Trim();

            using (var dbContext = new AccountDbContext())
            {
                var user = dbContext.Users.Include("Roles").Where(u => u.LoginName == loginName && u.Password == password && u.IsActive).FirstOrDefault();
                if (user != null)
                {
                    var ip = Fetch.UserIp;
                    loginInfo = dbContext.FindAll<LoginInfo>(p => p.LoginName == loginName && p.ClientIP == ip).FirstOrDefault();
                    if (loginInfo != null)
                    {
                        loginInfo.LastAccessTime = DateTime.Now;
                    }
                    else
                    {
                        loginInfo = new LoginInfo(user.ID, user.LoginName);
                        if (user.Roles != null && user.Roles.Count > 0 && user.Roles.Exists(u => u.Name == "系统管理员"))   //判断是否系统管理员
                            loginInfo.EnumLoginAccountType = (int)EnumLoginAccountType.Administrator;
                        loginInfo.ClientIP = ip;
                        loginInfo.BusinessPermissionList = user.BusinessPermissionList;
                        dbContext.Insert<LoginInfo>(loginInfo);
                    }
                }
            }

            return loginInfo;
        }

        public void Logout(Guid token)
        {
            using (var dbContext = new AccountDbContext())
            {
                var loginInfo = dbContext.FindAll<LoginInfo>(l => l.LoginToken == token).FirstOrDefault();
                if (loginInfo != null)
                {
                    dbContext.Delete<LoginInfo>(loginInfo);
                }
            }

            CacheHelper.Remove(string.Format(_LoginInfoKeyFormat, token));
        }

        public void ModifyPwd(User user)
        {
            user.Password = Encrypt.MD5(user.Password);

            using (var dbContext = new AccountDbContext())
            {
                if (dbContext.Users.Any(l => l.ID == user.ID && user.Password == l.Password))
                {
                    if (!string.IsNullOrEmpty(user.NewPassword))
                        user.Password = Encrypt.MD5(user.NewPassword);

                    dbContext.Update<User>(user);
                }
                else
                {
                    throw new BusinessException("Password", "原密码不正确！");
                }
            }
        }

        #endregion

        #region User
        public User GetUser(int id)
        {
            using (var dbContext = new AccountDbContext())
            {
                return dbContext.Users.Include("Roles").Include("DepartMent").Where(u => u.ID == id).SingleOrDefault();
            }
        }

        public User GetUserByIp(string ip)
        {
            using (var dbContext = new AccountDbContext())
            {
                return dbContext.Users.Include("Roles").Include("DepartMent").Where(o => o.IsActive).OrderByDescending(o => o.ID).FirstOrDefault<User>(u => u.IpAddress == ip);
            }
        }

        public IEnumerable<User> GetUserList(UserRequest request = null)
        {
            request = request ?? new UserRequest();

            using (var dbContext = new AccountDbContext())
            {
                IQueryable<User> queryList = dbContext.Users.Include("Roles").Include("DepartMent").Where(o => o.IsActive);

                if (!string.IsNullOrEmpty(request.LoginName))
                    queryList = queryList.Where(u => u.LoginName.Contains(request.LoginName));

                if (!string.IsNullOrEmpty(request.Mobile))
                    queryList = queryList.Where(u => u.Mobile.Contains(request.Mobile));

                if (request.Role != null)
                    queryList = queryList.Where(u => u.Roles.Count(o => o.ID == request.Role.ID) > 0);

                return queryList.OrderByDescending(u => u.ID).ToPagedList(request.PageIndex, request.PageSize);
            }
        }

        /// <summary>
        /// 查找拥有某一权限的所有用户（不包括系统管理员）
        /// </summary>
        /// <param name="perssion">权限</param>
        public IEnumerable<User> GetUserListByPermission(string perssion)
        {
            using (var db = new AccountDbContext())
            {
                var menus = PermissionMenu(perssion);
                if (menus == null || menus.Count() < 1)
                    throw new Exception(string.Format("权限'{0}'没有对应的菜单.", perssion));
                var id = menus.First().ID.ToString();
                var roles = db.Roles.Where(o => o.BusinessPermissionString.Contains(id));
                IQueryable<User> queryList = db.Users.Include("Roles").Include("DepartMent")
                    .Where(o => o.IsActive && o.ID != 7 && roles.Any(r => o.Roles.Contains(r)));    //排除系统管理员
                return queryList.ToList();
            }
        }

        public void SaveUser(User user)
        {
            using (var dbContext = new AccountDbContext())
            {
                if (user.ID > 0)
                {
                    dbContext.Update<User>(user);

                    var roles = dbContext.Roles.Where(r => user.RoleIds.Contains(r.ID)).ToList();
                    user.Roles = roles;
                    dbContext.SaveChanges();
                }
                else
                {
                    var existUser = dbContext.FindAll<User>(u => u.LoginName == user.LoginName);
                    if (existUser.Count > 0)
                    {
                        throw new BusinessException("LoginName", "此登录名已存在！");
                    }
                    else
                    {
                        dbContext.Insert<User>(user);
                        var roles = dbContext.Roles.Where(r => user.RoleIds.Contains(r.ID)).ToList();
                        user.Roles = roles;
                        dbContext.SaveChanges();
                    }
                }
            }
        }

        public void DeleteUser(List<int> ids)
        {
            using (var dbContext = new AccountDbContext())
            {
                dbContext.Users.Include("Roles").Include("DepartMent").Where(u => ids.Contains(u.ID)).ToList().ForEach(a => { a.Roles.Clear(); dbContext.Users.Remove(a); });
                dbContext.SaveChanges();
            }
        }
        #endregion

        #region Role
        public Role GetRole(int id)
        {
            using (var dbContext = new AccountDbContext())
            {
                return dbContext.Find<Role>(id);
            }
        }

        public IEnumerable<Role> GetRoleList(RoleRequest request = null)
        {
            request = request ?? new RoleRequest();
            using (var dbContext = new AccountDbContext())
            {
                IQueryable<Role> queryList = dbContext.Roles;

                if (!string.IsNullOrEmpty(request.RoleName))
                {
                    queryList = queryList.Where(u => u.Name.Contains(request.RoleName));
                }

                return queryList.OrderByDescending(u => u.ID).ToPagedList(request.PageIndex, request.PageSize);
            }
        }

        public void SaveRole(Role model)
        {
            using (var dbContext = new AccountDbContext())
            {
                if (model.ID > 0)
                {
                    dbContext.Update<Role>(model);
                }
                else
                {
                    dbContext.Insert<Role>(model);
                }
            }
        }

        public void DeleteRole(List<int> ids)
        {
            using (var dbContext = new AccountDbContext())
            {
                dbContext.Roles.Include("Users").Where(u => ids.Contains(u.ID)).ToList().ForEach(a => { a.Users.Clear(); dbContext.Roles.Remove(a); });
                dbContext.SaveChanges();
            }
        }
        #endregion

        #region Menu
        /// <summary>
        /// 查询单个对象
        /// </summary>
        public Menu GetMenu(int id)
        {
            using (var dbContext = new AccountDbContext())
            {
                return dbContext.Find<Menu>(id);
            }
        }
        /// <summary>
        /// 权限对应的菜单
        /// </summary>
        public IEnumerable<Menu> PermissionMenu(string Permission)
        {
            using (var dbContext = new AccountDbContext())
            {
                return dbContext.Menus.Where(o => o.Permission == Permission).ToList<Menu>();
            }
        }
        /// <summary>
        /// 查询列表(分页)
        /// </summary>
        public IEnumerable<Menu> GetMenuList(MenuRequest request = null)
        {
            request = request ?? new MenuRequest();
            using (var dbContext = new AccountDbContext())
            {
                IQueryable<Menu> queryList = dbContext.Menus;

                if (!string.IsNullOrEmpty(request.Name))
                    queryList = queryList.Where(o => o.Name.Contains(request.Name));
                if (request.ParentId.HasValue)
                    queryList = queryList.Where(o => o.ParentId == request.ParentId);

                return queryList.OrderBy(u => new { u.ParentId, u.Orderby }).ToPagedList(request.PageIndex, request.PageSize);
            }
        }
        /// <summary>
        /// 编辑保存
        /// </summary>
        public void SaveMenu(Menu model)
        {
            string cachingKey = "ConfigFile_AdminMenuConfig";
            using (var dbContext = new AccountDbContext())
            {
                model.ParentId = model.ParentId ?? 0;
                if (model.ID > 0)
                {
                    dbContext.Update<Menu>(model);
                }
                else
                {
                    dbContext.Insert<Menu>(model);
                }
                Caching.Remove(cachingKey); //删除菜单缓存
            }
        }
        /// <summary>
        /// 删除
        /// </summary>
        public void DeleteMenu(List<int> ids)
        {
            string cachingKey = "ConfigFile_AdminMenuConfig";
            using (var dbContext = new AccountDbContext())
            {
                dbContext.Menus.Where(u => ids.Contains(u.ID)).Delete();
                Caching.Remove(cachingKey);
            }
        }
        #endregion

    }
}
