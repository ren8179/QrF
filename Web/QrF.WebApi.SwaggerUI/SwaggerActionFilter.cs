using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace QrF.WebApi.SwaggerUI
{
    /// <summary>
    /// Determines if any request hit the Swagger route. Moves on if not, otherwise responds with JSON Swagger spec doc
    /// </summary>
    public class SwaggerActionFilter : ActionFilterAttribute
    {
        /// <summary>
        /// Executes each request to give either a JSON Swagger spec doc or passes through the request
        /// </summary>
        /// <param name="actionContext">Context of the action</param>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var docRequest = actionContext.ControllerContext.RouteData.Values.ContainsKey(SwaggerGen.SWAGGER);

            if (!docRequest)
            {
                base.OnActionExecuting(actionContext);
                return;
            }

            HttpResponseMessage response = new HttpResponseMessage();

            response.Content = new ObjectContent<ResourceListing>(
                getDocs(actionContext),
                actionContext.ControllerContext.Configuration.Formatters.JsonFormatter);

            actionContext.Response = response;
        }

        private ResourceListing getDocs(HttpActionContext actionContext)
        {
            var assemblyType = (actionContext.ActionDescriptor as ReflectedHttpActionDescriptor).MethodInfo.DeclaringType;
            var docProvider = new XmlCommentDocumentationProvider(); //(XmlCommentDocumentationProvider)GlobalConfiguration.Configuration.Services.GetDocumentationProvider();

            ResourceListing r = SwaggerGen.CreateResourceListing(actionContext);

            foreach (var api in GlobalConfiguration.Configuration.Services.GetApiExplorer().ApiDescriptions)
            {
                if (api.ActionDescriptor.ActionName.EndsWith("API"))//Ignore each Default API action
                    continue;

                string apiControllerName = api.ActionDescriptor.ControllerDescriptor.ControllerName;
                if (api.Route.Defaults.ContainsKey(SwaggerGen.SWAGGER) ||
                    apiControllerName.ToUpper().Equals(SwaggerGen.SWAGGER.ToUpper()))
                    continue;

                // Make sure we only report the current controller docs
                if (!apiControllerName.Equals(actionContext.ControllerContext.ControllerDescriptor.ControllerName))
                    continue;

                ResourceApi rApi = SwaggerGen.CreateResourceApi(api);
                r.apis.Add(rApi);

                ResourceApiOperation rApiOperation = SwaggerGen.CreateResourceApiOperation(r, api, docProvider);
                rApi.operations.Add(rApiOperation);

                foreach (var param in api.ParameterDescriptions)
                {
                    ResourceApiOperationParameter parameter = SwaggerGen.CreateResourceApiOperationParameter(r, api, param, docProvider);
                    rApiOperation.parameters.Add(parameter);
                }

                if (System.Configuration.ConfigurationManager.AppSettings["swagger:APITOKEN"] != null &&
                    System.Configuration.ConfigurationManager.AppSettings["swagger:APITOKEN"].Equals("true") &&
                    !api.ActionDescriptor.ActionName.EndsWith("API"))
                {
                    //添加Token
                    ResourceApiOperationParameter p = new ResourceApiOperationParameter();
                    p.name = "ApiToken";
                    p.description = "Api Token";
                    p.paramType = "path";
                    p.required = true;
                    p.dataType = "String";
                    rApiOperation.parameters.Insert(0, p);
                }

                SwaggerGen.CreateModel(r, api, docProvider);
                //r.models = new ResourceApiModel();
            }

            return r;
        }
    }
}
