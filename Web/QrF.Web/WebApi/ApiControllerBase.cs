using QrF.Account.Contract;
using QrF.Account.Service;
using QrF.Core.Config;
using System;
using System.Web.Http;

namespace QrF.Web.WebApi
{
    public class ApiControllerBase : ApiController
    {
        public virtual IAccountService AccountService
        {
            get
            {
                return ServiceContext.Current.AccountService;
            }
        }

        public AdminCookieContext CookieContext
        {
            get
            {
                return AdminCookieContext.Current;
            }
        }

        public AdminUserContext UserContext
        {
            get
            {
                return AdminUserContext.Current;
            }
        }

        public CachedConfigContext ConfigContext
        {
            get
            {
                return CachedConfigContext.Current;
            }
        }

        /// <summary>
        /// 用户Token，每次页面都会把这个UserToken标识发送到服务端认证
        /// </summary>
        public virtual Guid UserToken
        {
            get
            {
                return CookieContext.UserToken;
            }
        }
        /// <summary>
        /// 分页Size
        /// </summary>
        public virtual int PageSize
        {
            get
            {
                return 10;
            }
        }

        /// <summary>
        /// 登录后用户信息
        /// </summary>
        public virtual LoginInfo LoginInfo
        {
            get
            {
                return AdminUserContext.Current.LoginInfo;
            }
        }
        
    }
}
