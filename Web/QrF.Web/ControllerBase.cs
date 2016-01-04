using QrF.Account.Contract;
using QrF.Core.Log;
using QrF.Framework.Web;
using System;
using System.Collections.Generic;

namespace QrF.Web
{
    public abstract class ControllerBase : QrF.Framework.Web.ControllerBase
    {
        public virtual IAccountService AccountService
        {
            get
            {
                return ServiceContext.Current.AccountService;
            }
        }

        protected override void LogException(Exception exception, WebExceptionContext exceptionContext = null)
        {
            base.LogException(exception);
            var message = new
            {
                exception = exception.Message,
                exceptionContext = exceptionContext,
            };
            Log4NetHelper.Error(LoggerType.WebExceptionLog, message, exception);
        }

        public IDictionary<string, object> CurrentActionParameters { get; set; }

    }
}
