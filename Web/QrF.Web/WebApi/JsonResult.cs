using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QrF.Web.WebApi
{
    public class ReturnJsonResult
    {
        public static JsonResult<T> GetJsonResult<T>(int code, string msg, T data)
        {
            JsonResult<T> jsonResult = new JsonResult<T>();
            jsonResult.code = code;
            jsonResult.msg = msg;
            jsonResult.data = data;
            return jsonResult;
        }
    }

    /// <summary>
    /// 定义统计返回json格式数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonResult<T>
    {
        public int code { get; set; }
        public string msg { get; set; }
        public T data { get; set; }
    }
}
