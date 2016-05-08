using EntityFramework.Extensions;
using QrF.Account.Contract;
using QrF.Account.EntityFramework;
using QrF.Core.Cache;
using QrF.Core.Helper;
using QrF.Framework.Contract;
using QrF.Framework.DAL;
using QrF.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QrF.Account.Service
{
    public class AccountService : IAccountService
    {
        private readonly int _UserLoginTimeoutMinutes = 60;
        private readonly string _LoginInfoKeyFormat = "LoginInfo_{0}";
        private readonly int _RootMenuId = 1;

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
        public void UpdateLoginInfo(LoginInfo loginInfo)
        {
            using (var dbContext = new AccountDbContext())
            {
                if (loginInfo != null)
                {
                    dbContext.Update<LoginInfo>(loginInfo);
                    dbContext.SaveChanges();
                }
            }
        }

        public LoginInfo Login(string loginName, string password, string ip = null)
        {
            LoginInfo loginInfo = null;
            //password = Encrypt.MD5(password);
            loginName = loginName.Trim();

            using (var dbContext = new AccountDbContext())
            {
                var user = dbContext.Users.Include("Roles").Where(u => u.LoginName == loginName && u.Password == password && u.IsActive).FirstOrDefault();
                if (user != null)
                {
                    ip = ip ?? Fetch.UserIp;
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

        public void ModifyPwd(UserInput model)
        {
            using (var dbContext = new AccountDbContext())
            {
                var oldModel = GetUser(model.ID);
                if (oldModel.Password != model.Password)
                {
                    throw new BusinessException("Password", "原密码不正确！");
                }
                oldModel.Password = model.NewPassword;
                oldModel.Email = model.Email;
                oldModel.Mobile = model.Mobile;
                dbContext.Update<User>(oldModel);
            }
        }

        public Guid SaveVerifyCode(string verifyCodeText)
        {
            if (string.IsNullOrWhiteSpace(verifyCodeText))
                throw new BusinessException("verifyCode", "输入的验证码不能为空！");
            using (var dbContext = new AccountDbContext())
            {
                var verifyCode = new VerifyCode() { VerifyText = verifyCodeText, Guid = Guid.NewGuid() };
                dbContext.Insert<VerifyCode>(verifyCode);
                return verifyCode.Guid;
            }
        }

        public bool CheckVerifyCode(string verifyCodeText, Guid guid)
        {
            using (var dbContext = new AccountDbContext())
            {
                var verifyCode = dbContext.FindAll<VerifyCode>(v => v.Guid == guid && v.VerifyText == verifyCodeText).LastOrDefault();
                if (verifyCode != null)
                {
                    dbContext.VerifyCodes.Remove(verifyCode);
                    dbContext.SaveChanges();

                    //清除验证码大于2分钟还没请求的
                    var expiredTime = DateTime.Now.AddMinutes(-2);
                    dbContext.VerifyCodes.Where(v => v.CreateTime < expiredTime).Delete();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion

        #region User
        public User GetUser(int id)
        {
            using (var dbContext = new AccountDbContext())
            {
                return dbContext.Users.Include("Roles").Where(u => u.ID == id).SingleOrDefault();
            }
        }

        public User GetUserByIp(string ip)
        {
            using (var dbContext = new AccountDbContext())
            {
                return dbContext.Users.Include("Roles").Where(o => o.IsActive).OrderByDescending(o => o.ID).FirstOrDefault<User>(u => u.IpAddress == ip);
            }
        }

        public IEnumerable<User> GetUserPageList(UserRequest request = null)
        {
            request = request ?? new UserRequest();

            using (var dbContext = new AccountDbContext())
            {
                IQueryable<User> queryList = dbContext.Users.Include("Roles").Where(o => o.IsActive);

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
                IQueryable<User> queryList = db.Users.Include("Roles")
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
                dbContext.Users.Include("Roles").Where(u => ids.Contains(u.ID)).ToList().ForEach(a => { a.Roles.Clear(); dbContext.Users.Remove(a); });
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

        public IEnumerable<Role> GetRolePageList(RoleRequest request = null)
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
        /// 查询列表(未分页)
        /// </summary>
        public IEnumerable<Menu> GetMenuList(MenuRequest request = null)
        {
            request = request ?? new MenuRequest();
            using (var dbContext = new AccountDbContext())
            {
                IQueryable<Menu> queryList = dbContext.Menus.Include("Parent").Where(o => o.ID != _RootMenuId);

                if (!string.IsNullOrEmpty(request.Name))
                    queryList = queryList.Where(o => o.Name.Contains(request.Name));
                if (request.ParentId.HasValue)
                    queryList = queryList.Where(o => o.ParentId == request.ParentId);

                return queryList.OrderBy(u => u.Orderby).ToPagedList(request.PageIndex, request.PageSize);
            }
        }
        /// <summary>
        /// 查询列表(分页)
        /// </summary>
        public IEnumerable<Menu> GetMenuPageList(MenuRequest request = null)
        {
            request = request ?? new MenuRequest();
            using (var dbContext = new AccountDbContext())
            {
                IQueryable<Menu> queryList = dbContext.Menus.Include("Parent").Where(o=>o.ID!=_RootMenuId);

                if (!string.IsNullOrEmpty(request.Name))
                    queryList = queryList.Where(o => o.Name.Contains(request.Name));
                if (request.ParentId.HasValue)
                    queryList = queryList.Where(o => o.ParentId == request.ParentId);

                return queryList.OrderBy(u => u.Orderby).ToPagedList(request.PageIndex, request.PageSize);
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
                model.ParentId = model.ParentId ?? _RootMenuId;
                if (model.ID > 0)
                {
                    dbContext.Update<Menu>(model);
                }
                else
                {
                    model.Orderby = MaxOrderNumber(model.Parent ?? GetMenu(model.ParentId.Value));
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

        /// <summary>
        /// 菜单列表转为树形结构
        /// </summary>
        public AdminMenuConfig GetMenuConfig()
        {
            using (var dbContext = new AccountDbContext())
            {
                var menuList = dbContext.Menus.ToList();
                var value = new AdminMenuConfig();
                value.AdminMenuGroups = (from a in menuList
                                         where a.ParentId == _RootMenuId
                                         orderby a.Orderby
                                         select new AdminMenuGroup
                                         {
                                             Id = a.ID.ToString(),
                                             Name = a.Name,
                                             Url = a.Url,
                                             Icon = a.Icon,
                                             Code = a.Code,
                                             Permission = a.Permission,
                                             Info = a.Info,
                                             AdminMenuArray = (from b in menuList
                                                               where b.ParentId == a.ID
                                                               orderby b.Orderby
                                                               select new AdminMenu
                                                               {
                                                                   Id = b.ID.ToString(),
                                                                   Name = b.Name,
                                                                   Url = b.Url,
                                                                   Code = b.Code,
                                                                   Permission = b.Permission,
                                                                   Info = b.Info
                                                               }).ToList<AdminMenu>()
                                         }).ToArray<AdminMenuGroup>();
                return value;
            }
        }

        public IEnumerable<Menu> GetUserMenuList(Guid token, int parentId)
        {
            using (var db = new AccountDbContext())
            {
                var loginInfo = GetLoginInfo(token);
                if (loginInfo == null)
                    throw new NullReferenceException("用户不存在");
                var menus = GetMenuList(new MenuRequest() { ParentId = parentId }).Where(o => loginInfo.BusinessPermissionList.Contains(o.ID));
                return menus;
            }
        }

        public AdminMenuConfig GetUserMenuList(Guid token)
        {
            using (var db = new AccountDbContext())
            {
                var loginInfo = GetLoginInfo(token);
                if (loginInfo == null)
                    throw new NullReferenceException("用户不存在");
                var menus = db.Menus.Where(o => loginInfo.BusinessPermissionList.Contains(o.ID)).ToList();
                var value = new AdminMenuConfig();
                value.AdminMenuGroups = (from a in menus
                                         where a.ParentId == _RootMenuId
                                         orderby a.Orderby
                                         select new AdminMenuGroup
                                         {
                                             Id = a.ID.ToString(),
                                             Name = a.Name,
                                             Url = a.Url,
                                             Icon = a.Icon,
                                             Code = a.Code,
                                             Permission = a.Permission,
                                             Info = a.Info,
                                             AdminMenuArray = (from b in menus
                                                               where b.ParentId == a.ID
                                                               orderby b.Orderby
                                                               select new AdminMenu
                                                               {
                                                                   Id = b.ID.ToString(),
                                                                   Name = b.Name,
                                                                   Url = b.Url,
                                                                   Icon = b.Icon,
                                                                   Code = b.Code,
                                                                   Permission = b.Permission,
                                                                   Info = b.Info
                                                               }).ToList<AdminMenu>()
                                         }).ToArray<AdminMenuGroup>();
                return value;
            }
        }

        /// <summary>
        /// 自动生成排序编号
        /// </summary>
        private string MaxOrderNumber(Menu parent)
        {
            using (var dbContext = new AccountDbContext())
            {
                var orderNum = parent.Orderby;
                var count = dbContext.Menus.Where(o => o.ParentId == parent.ID).Count()+1;
                orderNum += count < 10 ? "0" + count.ToString() : count.ToString();
                return orderNum;
            }
        }

        #endregion

        #region AuditLog
        /// <summary>
        /// 查询单个对象
        /// </summary>
        public AuditLog GetAuditLog(int id)
        {
            using (var db = new LogDbContext())
            {
                return db.Find<AuditLog>(id);
            }
        }

        /// <summary>
        /// 查询列表(未分页)
        /// </summary>
        public IEnumerable<AuditLog> GetAuditLogList(AuditLogRequest request = null)
        {
            request = request ?? new AuditLogRequest();
            using (var dbContext = new LogDbContext())
            {
                IQueryable<AuditLog> queryList = dbContext.AuditLogs;
                
                return queryList.OrderByDescending(u => u.CreateTime).ToPagedList(request.PageIndex, request.PageSize);
            }
        }
        /// <summary>
        /// 查询列表(分页)
        /// </summary>
        public IEnumerable<AuditLog> GetAuditLogPageList(AuditLogRequest request = null)
        {
            request = request ?? new AuditLogRequest();
            using (var dbContext = new LogDbContext())
            {
                IQueryable<AuditLog> queryList = dbContext.AuditLogs;
                
                return queryList.OrderByDescending(u => u.CreateTime).ToPagedList(request.PageIndex, request.PageSize);
            }
        }
        
        #endregion

    }
}
