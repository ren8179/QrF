using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace QrF.Web
{
    /// <summary>
    /// 用于权限点认证，标记在Action上面
    /// </summary>
    public class PermissionAttribute : FilterAttribute, IActionFilter
    {
        public List<int> Permissions { get; set; }

        public PermissionAttribute(params String[] parameters)
        {
            Permissions = Permissions ?? new List<int>();
            foreach (var item in parameters)
            {
                var menus = ServiceContext.Current.AccountService.PermissionMenu(item);
                if (menus != null && menus.Count() > 0)
                    Permissions.AddRange(menus.Select(o => o.ID));
            }
        }
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //throw new NotImplementedException();
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //throw new NotImplementedException();
        }
    }
}
