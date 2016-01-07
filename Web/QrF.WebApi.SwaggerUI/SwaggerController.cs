using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace QrF.WebApi.SwaggerUI
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SwaggerController : ApiController
    {
        /// <summary>
        /// Get the resource description of the api for swagger documentation
        /// </summary>
        /// <remarks>It is very convenient to have this information available for generating clients. This is the entry point for the swagger UI
        /// </remarks>
        /// <returns>JSON document representing structure of API</returns>
        public HttpResponseMessage Get(int type = 1)
        {
            var docProvider = (XmlCommentDocumentationProvider)GlobalConfiguration.Configuration.Services.GetDocumentationProvider();

            ResourceListing r = SwaggerGen.CreateResourceListing(ControllerContext);
            List<string> uniqueControllers = new List<string>();

            foreach (var api in GlobalConfiguration.Configuration.Services.GetApiExplorer().ApiDescriptions)
            {
                string controllerName = api.ActionDescriptor.ControllerDescriptor.ControllerName;
                if (uniqueControllers.Contains(controllerName) ||
                      controllerName.ToUpper().Equals(SwaggerGen.SWAGGER.ToUpper())
                    || controllerName.ToLower().StartsWith("abp")
                    ) continue;

                uniqueControllers.Add(controllerName);

                //添加API
                ResourceApi rApi = SwaggerGen.CreateResourceApi(api);
                r.apis.Add(rApi);
            }

            HttpResponseMessage resp = new HttpResponseMessage();

            resp.Content = new ObjectContent<ResourceListing>(r, ControllerContext.Configuration.Formatters.JsonFormatter);

            return resp;
        }
    }
}
