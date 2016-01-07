using QrF.Core.Log;
using QrF.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace QrF.Web.WebApi.Filter
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class ExceptionAttribute : ExceptionFilterAttribute,IExceptionFilter
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            base.OnException(context);
            var exception = context.Exception;
            var message = new
            {
                IP = Fetch.UserIp,
                CurrentUrl = Fetch.CurrentUrl,
                Headers = context.Request == null ? string.Empty : context.Request.Content.Headers.ToString(),
                RefUrl = (context.Request == null) ? string.Empty : context.Request.RequestUri.AbsoluteUri,
                RouteData = (context.Request == null) ? null : context.Request.GetRouteData().Values,
                QueryData = (context.Request == null) ? null : context.Request.GetQueryNameValuePairs(),
                exception = exception.Message
            };
            Log4NetHelper.Error(LoggerType.WebExceptionLog, message, exception);
            context.Response = new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError };
        }
    }
}
