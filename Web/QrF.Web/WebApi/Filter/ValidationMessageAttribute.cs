using QrF.Account.Contract;
using QrF.Core.Config;
using QrF.Framework.Contract;
using QrF.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace QrF.Web.WebApi.Filter
{
    /// <summary>
    /// 请求参数验证，统一处理筛选器
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class ValidationMessageAttribute : ActionFilterAttribute,IActionFilter
    {
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
        /// 登录后用户信息里的用户权限
        /// </summary>
        public virtual List<int> PermissionList
        {
            get
            {
                var permissionList = new List<int>();
                if (AdminUserContext.Current.LoginInfo == null)
                    return permissionList;
                return AdminUserContext.Current.LoginInfo.BusinessPermissionList;
            }
        }


        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var noAuthorizeAttributes = actionContext.ActionDescriptor.GetCustomAttributes<AuthorizeIgnoreAttribute>(false);
            if (noAuthorizeAttributes.Count > 0)
                return;
            if (AdminUserContext.Current.LoginInfo == null)
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("请登录！"),
                    ReasonPhrase = "请登录！"
                };
                return;
            }
            base.OnActionExecuting(actionContext);
            //在方法执行前，附加上PageSize值
            actionContext.ActionArguments.Values.Where(v => v is Request).ToList().ForEach(v => ((Request)v).PageSize = this.PageSize);
            bool hasPermission = true;
            var permissionAttributes = actionContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<PermissionAttribute>(false).Cast<PermissionAttribute>();
            permissionAttributes = actionContext.ActionDescriptor.GetCustomAttributes<PermissionAttribute>(false).Cast<PermissionAttribute>().Union(permissionAttributes);
            var attributes = permissionAttributes as IList<PermissionAttribute> ?? permissionAttributes.ToList();
            if (permissionAttributes != null && attributes.Count() > 0)
            {
                hasPermission = false;
                foreach (var attr in attributes)
                {
                    foreach (var permission in attr.Permissions)
                    {
                        if (PermissionList.Contains(permission))
                        {
                            hasPermission = true;
                            break;
                        }
                    }
                }

                if (!hasPermission)
                {
                    actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        Content = new StringContent("没有权限！"),
                        ReasonPhrase = "没有权限！"
                    };
                    return;
                }
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
        }

    }
}
