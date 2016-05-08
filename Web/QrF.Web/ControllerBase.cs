using Newtonsoft.Json;
using QrF.Account.Service;
using QrF.Core.Log;
using QrF.Framework.Contract;
using QrF.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace QrF.Web
{
    public abstract class ControllerBase : Controller
    {
        /// <summary>
        /// 操作人，传IP....到后端记录
        /// </summary>
        public virtual Operater Operater
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 分页大小
        /// </summary>
        public virtual int PageSize
        {
            get
            {
                return 15;
            }
        }

        protected ContentResult JsonP(string callback, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return this.Content(string.Format("{0}({1})", callback, json));
        }

        /// <summary>
        /// 返回成功的Json数据
        /// </summary>
        public JsonResult JsonSuccess(string msg = "")
        {
            var json = new
            {
                valid = true,
                msg = msg
            };
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 返回失败的Json数据
        /// </summary>
        public JsonResult JsonError(string msg = "")
        {
            var json = new
            {
                valid = false,
                msg = msg
            };
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        ///  警告并且历史返回
        /// </summary>
        /// <param name="notice"></param>
        /// <returns></returns>
        public ContentResult Back(string notice)
        {
            var content = new StringBuilder("<script>");
            if (!string.IsNullOrEmpty(notice))
                content.AppendFormat("alert('{0}');", notice);
            content.Append("history.go(-1)</script>");
            return this.Content(content.ToString());
        }


        public ContentResult PageReturn(string msg, string url = null)
        {
            var content = new StringBuilder("<script type='text/javascript'>");
            if (!string.IsNullOrEmpty(msg))
                content.AppendFormat("alert('{0}');", msg);
            if (string.IsNullOrWhiteSpace(url))
                url = Request.Url.ToString();
            content.Append("window.location.href='" + url + "'</script>");
            return this.Content(content.ToString());
        }

        /// <summary>
        /// 转向到一个提示页面，然后自动返回指定的页面
        /// </summary>
        /// <param name="notice"></param>
        /// <param name="redirect"></param>
        /// <returns></returns>
        public ContentResult Stop(string notice, string redirect, bool isAlert = false)
        {
            var content = "<meta http-equiv='refresh' content='1;url=" + redirect + "' /><body style='margin-top:0px;color:red;font-size:24px;'>" + notice + "</body>";

            if (isAlert)
                content = string.Format("<script>AlertN('{0}'); setTimeout(\"window.location.href='{1}'\",1000);</script>", notice, redirect);

            return this.Content(content);
        }

        /// <summary>
        /// 在方法执行前更新操作人
        /// </summary>
        /// <param name="filterContext"></param>
        public virtual void UpdateOperater(ActionExecutingContext filterContext)
        {
            if (this.Operater == null)
                return;

            WCFContext.Current.Operater = this.Operater;
        }

        public virtual void ClearOperater()
        {
            //TODO
        }

        /// <summary>
        /// AOP拦截，在Action执行后
        /// </summary>
        /// <param name="filterContext">filter context</param>
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            if (!filterContext.RequestContext.HttpContext.Request.IsAjaxRequest() && !filterContext.IsChildAction)
                RenderViewData();

            this.ClearOperater();
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            this.UpdateOperater(filterContext);
            base.OnActionExecuting(filterContext);

            //在方法执行前，附加上PageSize值
            filterContext.ActionParameters.Values.Where(v => v is Request).ToList().ForEach(v => ((Request)v).PageSize = this.PageSize);
        }

        /// <summary>
        /// 产生一些视图数据
        /// </summary>
        protected virtual void RenderViewData()
        {
        }

        /// <summary>
        /// 当前Http上下文信息，用于写Log或其他作用
        /// </summary>
        public WebExceptionContext WebExceptionContext
        {
            get
            {
                var exceptionContext = new WebExceptionContext
                {
                    IP = Fetch.UserIp,
                    CurrentUrl = Fetch.CurrentUrl,
                    RefUrl = (Request == null || Request.UrlReferrer == null) ? string.Empty : Request.UrlReferrer.AbsoluteUri,
                    IsAjaxRequest = (Request == null) ? false : Request.IsAjaxRequest(),
                    FormData = (Request == null) ? null : Request.Form,
                    QueryData = (Request == null) ? null : Request.QueryString,
                    RouteData = (Request == null || Request.RequestContext == null || Request.RequestContext.RouteData == null) ? null : Request.RequestContext.RouteData.Values
                };

                return exceptionContext;
            }
        }

        /// <summary>
        /// 发生异常写Log
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnException(ExceptionContext filterContext)
        {
            var e = filterContext.Exception;
            LogException(e, this.WebExceptionContext);
        }

        public virtual IAccountService AccountService
        {
            get
            {
                return ServiceContext.Current.AccountService;
            }
        }

        protected virtual void LogException(Exception exception, WebExceptionContext exceptionContext = null)
        {
            var message = new
            {
                exception = exception.Message,
                exceptionContext = exceptionContext,
            };
            Log4NetHelper.Error(LoggerType.WebExceptionLog, message, exception);
        }

        public IDictionary<string, object> CurrentActionParameters { get; set; }

    }
    public class WebExceptionContext
    {
        public string IP { get; set; }
        public string CurrentUrl { get; set; }
        public string RefUrl { get; set; }
        public bool IsAjaxRequest { get; set; }
        public NameValueCollection FormData { get; set; }
        public NameValueCollection QueryData { get; set; }
        public RouteValueDictionary RouteData { get; set; }
    }
}
