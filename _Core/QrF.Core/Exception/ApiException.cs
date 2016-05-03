using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QrF.Core
{
    public class ApiException : Exception
    {
        public string ErrorCode { get; set; }
        public ApiException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
