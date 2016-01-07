using QrF.Core.Cache;

namespace QrF.Web
{
    public class AdminUserContext : UserContext
    {
        public AdminUserContext(): base(AdminCookieContext.Current){}

        public AdminUserContext(IAuthCookie authCookie): base(authCookie){}

        public static AdminUserContext Current
        {
            get
            {
                return CacheHelper.GetItem<AdminUserContext>();
            }
        }
    }
}
