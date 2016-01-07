using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Xml.XPath;

namespace QrF.WebApi.SwaggerUI
{
    public static class SwaggerGen
    {
        public const string SWAGGER = "swagger";
        public const string SWAGGER_VERSION = "2.0";
        public const string FROMURI = "FromUri";
        public const string FROMBODY = "FromBody";
        public const string QUERY = "query";
        public const string PATH = "path";
        public const string BODY = "body";
        public const string NODESCRIPTION = "暂无说明";

        public static ConcurrentDictionary<string, XPathNavigator> XmlCache = new ConcurrentDictionary<string, XPathNavigator>();

        /// <summary>
        /// Create a resource listing
        /// </summary>
        /// <param name="actionContext">Current action context</param>
        /// <param name="includeResourcePath">Should the resource path property be included in the response</param>
        /// <returns>A resource Listing</returns>
        public static ResourceListing CreateResourceListing(HttpActionContext actionContext, bool includeResourcePath = true)
        {
            return CreateResourceListing(actionContext.ControllerContext, includeResourcePath);
        }

        /// <summary>
        /// Create a resource listing
        /// </summary>
        /// <param name="actionContext">Current controller context</param>
        /// <param name="includeResourcePath">Should the resource path property be included in the response</param>
        /// <returns>A resrouce listing</returns>
        public static ResourceListing CreateResourceListing(HttpControllerContext controllerContext, bool includeResourcePath = false)
        {
            Uri uri = controllerContext.Request.RequestUri;
            var loadAssembly = System.Configuration.ConfigurationManager.AppSettings["swagger:APIAseembly"];

            var version = Assembly.GetCallingAssembly().GetType().Assembly.GetName().Version.ToString();
            if (!string.IsNullOrEmpty(loadAssembly))
                version = Assembly.Load(loadAssembly).GetName().Version.ToString();
            ResourceListing rl = new ResourceListing()
            {
                apiVersion = version,
                swaggerVersion = SWAGGER_VERSION,
                basePath = uri.GetLeftPart(UriPartial.Authority) + HttpRuntime.AppDomainAppVirtualPath.TrimEnd('/'),
                apis = new List<ResourceApi>()
            };

            if (includeResourcePath) rl.resourcePath = controllerContext.ControllerDescriptor.ControllerName;

            return rl;
        }

        /// <summary>
        /// Create an api element 
        /// </summary>
        /// <param name="api">Description of the api via the ApiExplorer</param>
        /// <returns>A resource api</returns>
        public static ResourceApi CreateResourceApi(ApiDescription api)
        {
            var path = "/" + api.RelativePath;

            if (!api.ActionDescriptor.ActionName.EndsWith("API"))
            {
                if (System.Configuration.ConfigurationManager.AppSettings["swagger:APITOKEN"] != null &&
                    System.Configuration.ConfigurationManager.AppSettings["swagger:APITOKEN"].Equals("true"))
                {
                    if (path.Contains("?"))
                        path += "&ApiToken={APITOKEN}";
                    else
                        path += "?ApiToken={APITOKEN}";
                }
            }

            ResourceApi rApi = new ResourceApi()
            {
                path = path,
                description = GetDocumentation((api.ActionDescriptor as ReflectedHttpActionDescriptor).MethodInfo), //string.IsNullOrWhiteSpace(api.Documentation) ? NODESCRIPTION : api.Documentation,
                operations = new List<ResourceApiOperation>()
            };

            return rApi;
        }

        /// <summary>
        /// Creates an api operation
        /// </summary>
        /// <param name="api">Description of the api via the ApiExplorer</param>
        /// <param name="docProvider">Access to the XML docs written in code</param>
        /// <returns>An api operation</returns>
        public static ResourceApiOperation CreateResourceApiOperation(ResourceListing r, ApiDescription api, XmlCommentDocumentationProvider docProvider)
        {
            ResourceApiOperation rApiOperation = new ResourceApiOperation()
            {
                httpMethod = api.HttpMethod.ToString(),
                nickname = docProvider.GetNickname(api.ActionDescriptor),
                responseClass = docProvider.GetResponseClass(api.ActionDescriptor),
                summary = docProvider.GetDocumentation(api.ActionDescriptor),
                notes = docProvider.GetNotes(api.ActionDescriptor),
                parameters = new List<ResourceApiOperationParameter>(),

            };
            if (string.IsNullOrEmpty(rApiOperation.notes) || rApiOperation.notes.Equals("No Documentation Found."))
                rApiOperation.notes = rApiOperation.summary;
            return rApiOperation;
        }

        public static void CreateModel(ResourceListing r, ApiDescription api, XmlCommentDocumentationProvider docProvider)
        {
            if (r.models == null)
                r.models = new ConcurrentDictionary<string, object>();

            ReflectedHttpActionDescriptor reflectedActionDescriptor = api.ActionDescriptor as ReflectedHttpActionDescriptor;
            if (reflectedActionDescriptor != null)
            {
                //this function will be called generating swagger API json
                //limitations
                //-only generating swagger models (API return type documentation) for return type is ApiResponse
                //--most calls fall into these 3 categories: 
                //--ApiResponse<PageData<T>> 
                //--ApiResponse<T>
                //--ApiResponse<T> where T is simple type such as object, string, int, etc.
                //-only generate detail when T is DataObject 

                //implementation notes
                //-get the actual T
                //--when T is DataObject, add into a queue (i.e. a FIFO Stack) for next step processing
                //-loop until queue is empty
                //--process (add model details) for the first item in queue, remove it after processed
                //--during the processing, if found any property is type of DataObject, add the nested type into queue
                //-the reason to use a while loop with a queue is to avoid using recursive implementation which will throw a stack-overflow exception when it's too many nested types

                var returnType = reflectedActionDescriptor.MethodInfo.ReturnType;
                List<Type> queue = new List<Type>();//process queue
                AddTypeToProcessQueue(r, returnType, queue);
                //parameter handling
                var parameters = reflectedActionDescriptor.MethodInfo.GetParameters();
                if (parameters != null && parameters.Length > 0)
                {
                    foreach (var p in parameters)
                    {
                        if (p.ParameterType.IsGenericType)
                        {
                            var dataObjType = p.ParameterType.GetGenericArguments()[0];
                            AddTypeToProcessQueue(r, dataObjType, queue);
                        }
                        else
                            AddTypeToProcessQueue(r, p.ParameterType, queue);
                    }
                }
                if (returnType.IsGenericType)
                {
                    Type[] types = returnType.GetGenericArguments();
                    if (types[0].Name.Equals("PageData`1"))
                    {
                        var dataObjType = types[0].GetGenericArguments()[0];
                        AddTypeToProcessQueue(r, dataObjType, queue);
                        //ApiResponse_PageData_xxxxx
                        var model = AddModelApiResponse(r, "ApiResponse_PageData_" + dataObjType.Name);
                        if (model != null)
                        {
                            var p = new ResourceApiModelProperty();
                            p.type = "Array";
                            p.description = "Page data set";
                            var items = new Dictionary<string, string>();
                            items.Add("$ref", "PageData_" + dataObjType.Name);
                            p.items = items;
                            model.properties.Add("Results", p);
                        }
                        if (!r.models.ContainsKey("PageData_" + dataObjType.Name))
                        {
                            model = new ResourceApiModel();
                            model.id = "PageData_" + dataObjType.Name;
                            model.properties = new Dictionary<string, ResourceApiModelProperty>();
                            var p = new ResourceApiModelProperty();
                            p.type = "int";
                            p.description = "total no of qualified data";
                            model.properties.Add("TotalRecords", p);

                            p = new ResourceApiModelProperty();
                            p.type = "bool";
                            p.description = "whether has next page";
                            model.properties.Add("HasNextPage", p);

                            p = new ResourceApiModelProperty();
                            p.type = "int";
                            p.description = "curent page index, start from 0";
                            model.properties.Add("PageIndex", p);

                            p = new ResourceApiModelProperty();
                            p.type = "int";
                            p.description = "how many records in one page";
                            model.properties.Add("PageSize", p);

                            p = new ResourceApiModelProperty();
                            p.type = "Array";
                            p.description = "A page of data";
                            var items = new Dictionary<string, string>();
                            items.Add("$ref", dataObjType.Name);
                            p.items = items;
                            model.properties.Add("Results", p);

                            r.models.TryAdd("PageData_" + dataObjType.Name, model);

                        }

                    }
                    else if (returnType.Name.Equals("Page`1"))
                    {
                        var dataObjType = types[0];
                        AddTypeToProcessQueue(r, dataObjType, queue);
                        var paging = returnType.GetProperty("Paging");
                        if (paging != null)
                        {//add paging type manually
                            AddTypeToProcessQueue(r, paging.PropertyType, queue);
                        }
                        //Page_xxxxx

                        if (!r.models.ContainsKey("Page_" + dataObjType.Name))
                        {
                            var model = new ResourceApiModel();

                            model.id = "Page_" + dataObjType.Name;
                            model.properties = new Dictionary<string, ResourceApiModelProperty>();

                            var p = new ResourceApiModelProperty();
                            p.type = "Paging";
                            p.description = "Paging info";

                            model.properties.Add("Paging", p);

                            p = new ResourceApiModelProperty();
                            p.type = "Array";
                            p.description = "A page of data";
                            var items = new Dictionary<string, string>();
                            items.Add("$ref", dataObjType.Name);
                            p.items = items;
                            model.properties.Add("Records", p);
                            r.models.TryAdd("Page_" + dataObjType.Name, model);
                        }


                    }
                    else
                    {
                        var dataObjType = types[0];
                        AddTypeToProcessQueue(r, dataObjType, queue);

                    }


                }
                //process queue
                while (queue.Count > 0)
                {
                    var type = queue[0];
                    AddModelDataObject(r, queue[0], queue);
                    queue.Remove(type);
                }
            }

        }
        private static void AddTypeToProcessQueue(ResourceListing r, Type type, List<Type> queue)
        {
            if (!queue.Contains(type) && //already in queue
                !r.models.ContainsKey(type.Name)//already handled
                && !IsExceptType(type))//only process Data Object
                queue.Add(type);
        }

        public static bool IsExceptType(Type type)
        {
            return type.IsPrimitive || type.IsValueType || type.FullName.StartsWith("System");
        }

        private static void AddModelDataObject(ResourceListing r, Type type, List<Type> queue)
        {
            if (!r.models.ContainsKey(type.Name))
            {
                var model = new ResourceApiModel();
                model.id = type.Name;
                model.properties = new Dictionary<string, ResourceApiModelProperty>();
                foreach (var pi in type.GetProperties())
                {
                    var ignore = pi.GetCustomAttribute<System.Runtime.Serialization.IgnoreDataMemberAttribute>();
                    if (ignore != null)
                        continue;
                    var p = new ResourceApiModelProperty();
                    switch (pi.PropertyType.Name)
                    {
                        case "List`1":
                            var dataObjType = pi.PropertyType.GetGenericArguments()[0];
                            AddTypeToProcessQueue(r, dataObjType, queue);
                            p.type = "Array";
                            var items = new Dictionary<string, string>();
                            items.Add("$ref", dataObjType.Name);
                            p.items = items;
                            break;
                        case "Nullable`1":
                            var dd = pi.PropertyType.GetGenericArguments()[0];
                            p.type = dd.Name;
                            AddTypeToProcessQueue(r, dd, queue);
                            break;
                        case "Dictionary`2":
                            p.type = string.Format("Array[{0},{1}]"
                         , pi.PropertyType.GetGenericArguments()[0].Name
                         , pi.PropertyType.GetGenericArguments()[1].Name);
                            break;
                        default:
                            p.type = pi.PropertyType.Name;
                            AddTypeToProcessQueue(r, pi.PropertyType, queue);
                            break;
                    }

                    //get description from xml
                    p.description = GetDocumentation(pi);
                    if (string.IsNullOrWhiteSpace(p.description))
                        p.description = NODESCRIPTION;
                    if (!model.properties.ContainsKey(pi.Name))
                        model.properties.Add(pi.Name, p);
                    //check if this is a nested data obj type, put into queue if yes

                }

                r.models.TryAdd(type.Name, model);
            }
        }

        private static void AddModelDataObject(ResourceListing r, Type type)
        {
            if (r.models != null && !r.models.ContainsKey(type.Name))
            {
                var model = new ResourceApiModel();
                model.id = type.Name;
                model.properties = new Dictionary<string, ResourceApiModelProperty>();
                foreach (var pi in type.GetProperties())
                {
                    var ignore = pi.GetCustomAttribute<System.Runtime.Serialization.IgnoreDataMemberAttribute>();
                    if (ignore != null)
                        continue;
                    var p = new ResourceApiModelProperty();

                    switch (pi.PropertyType.Name)
                    {
                        case "List`1":
                            var dataObjType = pi.PropertyType.GetGenericArguments()[0];

                            p.type = "Array";
                            var items = new Dictionary<string, string>();
                            items.Add("$ref", dataObjType.Name);
                            p.items = items;
                            break;
                        case "Nullable`1":
                            var dd = pi.PropertyType.GetGenericArguments()[0];
                            p.type = dd.Name;
                            break;
                        case "Dictionary`2":
                            p.type = string.Format("Array[{0},{1}]"
                         , pi.PropertyType.GetGenericArguments()[0].Name
                         , pi.PropertyType.GetGenericArguments()[1].Name);
                            break;
                        default:
                            p.type = pi.PropertyType.Name;
                            if (!IsExceptType(pi.PropertyType))
                            {
                                AddModelDataObject(r, pi.PropertyType);
                            }
                            break;
                    }

                    //get description from xml
                    p.description = GetDocumentation(pi);
                    if (string.IsNullOrWhiteSpace(p.description))
                        p.description = NODESCRIPTION;
                    if (!model.properties.ContainsKey(pi.Name))
                        model.properties.Add(pi.Name, p);
                    //check if this is a nested data obj type, put into queue if yes
                }
                r.models.TryAdd(type.Name, model);
            }
        }

        public static XPathNavigator GetXPathDocument(Type type)
        {
            //load xml
            var typeName = type.Assembly.GetName().Name;
            System.Xml.XPath.XPathDocument xpath;
            if (!XmlCache.ContainsKey(typeName))
            {
                var path = HttpContext.Current.Server.MapPath(string.Format("~/bin/{0}.xml", typeName));
                if (!System.IO.File.Exists(path))
                    return null;
                xpath = new System.Xml.XPath.XPathDocument(path);
                if (!XmlCache.ContainsKey(type.Assembly.GetName().Name))
                {
                    //double check
                    XmlCache.TryAdd(type.Assembly.GetName().Name, xpath.CreateNavigator());
                }
            }

            return XmlCache.ContainsKey(typeName) ? XmlCache[typeName] : null;
        }

        public static string GetDocumentation<T>(T pi, Type belongToType = null)
            where T : MemberInfo
        {
            if (belongToType == null)
                belongToType = pi.DeclaringType;

            var navi = GetXPathDocument(belongToType);
            if (navi == null)
                return NODESCRIPTION;

            XPathNavigator node = null;
            if (pi is MethodInfo)
            {
                var parms = (pi as MethodInfo).GetParameters();

                node = navi.SelectSingleNode(string.Format("/doc/members/member[@name='M:{0}.{1}({2})']/summary",
                   belongToType.FullName, pi.Name, parms.Length > 0 ? string.Join(",", parms.Select(x => x.ParameterType.FullName)) : string.Empty));
            }
            else if (pi is PropertyInfo)
            {
                node = navi.SelectSingleNode(string.Format("/doc/members/member[@name='P:{0}']/summary", belongToType.FullName + "." + pi.Name));
            }
            if (node != null)
                return node.Value.Trim();
            //else if (typeof(IDto).IsAssignableFrom(belongToType) || typeof(ISysIdEntity).IsAssignableFrom(belongToType))
            //    return GetActionDocumentation(pi, belongToType.BaseType);//maybe the field is from base class
            else
                return NODESCRIPTION;
        }

        private static ResourceApiModel AddModelApiResponse(ResourceListing r, string targetTypeName)
        {
            if (!r.models.ContainsKey(targetTypeName))
            {
                var model = new ResourceApiModel();
                model.id = targetTypeName;
                model.properties = new Dictionary<string, ResourceApiModelProperty>();
                var p = new ResourceApiModelProperty();
                p.type = "bool";
                p.description = "Indicates whether the API call is success or not";
                model.properties.Add("Success", p);

                p = new ResourceApiModelProperty();
                p.type = "string";
                p.description = "Additional message for API result";
                model.properties.Add("Message", p);

                p = new ResourceApiModelProperty();
                p.type = "Exception";
                p.description = "In case catch an exception";
                model.properties.Add("Exception", p);


                r.models.TryAdd(targetTypeName, model);
                return model;
            }
            else
                return null;
        }

        /// <summary>
        /// Creates an operation parameter
        /// </summary>
        /// <param name="api">Description of the api via the ApiExplorer</param>
        /// <param name="param">Description of a parameter on an operation via the ApiExplorer</param>
        /// <param name="docProvider">Access to the XML docs written in code</param>
        /// <returns>An operation parameter</returns>
        public static ResourceApiOperationParameter CreateResourceApiOperationParameter(ResourceListing r, ApiDescription api, ApiParameterDescription param, XmlCommentDocumentationProvider docProvider)
        {
            string paramType = (param.Source.ToString().Equals(FROMURI)) ? QUERY : BODY;
            var dataType = param.ParameterDescriptor.ParameterType.Name;
            switch (dataType)
            {
                case "List`1":
                    var dataObjType = param.ParameterDescriptor.ParameterType.GetGenericArguments()[0];
                    dataType = string.Format("Array[{0}]", dataObjType.Name);
                    break;
                case "Nullable`1":
                    var dd = param.ParameterDescriptor.ParameterType.GetGenericArguments()[0];
                    dataType = dd.Name;
                    break;
                case "Dictionary`2":

                    dataType = string.Format("Array[{0},{1}]", param.ParameterDescriptor.ParameterType.GetGenericArguments()[0].Name,
                        param.ParameterDescriptor.ParameterType.GetGenericArguments()[1].Name);
                    break;
                default:
                    if (!IsExceptType(param.ParameterDescriptor.ParameterType))
                    {
                        AddModelDataObject(r, param.ParameterDescriptor.ParameterType);
                    }
                    break;
            }
            ResourceApiOperationParameter parameter = new ResourceApiOperationParameter()
            {
                paramType = (paramType == "query" && api.RelativePath.IndexOf("{" + param.Name + "}") > -1) ? PATH : paramType,
                name = param.Name,
                description = param.Name.Equals("sessionKey") ? "Login session" : (string.IsNullOrWhiteSpace(param.Documentation) ? NODESCRIPTION : param.Documentation),
                dataType = dataType,
                required = docProvider.GetRequired(param.ParameterDescriptor)
            };

            return parameter;
        }
    }

    public class ResourceListing
    {
        public string apiVersion { get; set; }
        public string swaggerVersion { get; set; }
        public string basePath { get; set; }
        public string resourcePath { get; set; }
        public List<ResourceApi> apis { get; set; }
        public ConcurrentDictionary<string, object> models { get; set; }
    }

    public class ResourceApi
    {
        public string path { get; set; }
        public string description { get; set; }
        public List<ResourceApiOperation> operations { get; set; }

    }

    public class ResourceApiOperation
    {
        public string httpMethod { get; set; }
        public string nickname { get; set; }
        public string responseClass { get; set; }
        public string summary { get; set; }
        public string notes { get; set; }
        public List<ResourceApiOperationParameter> parameters { get; set; }

    }

    public class ResourceApiOperationParameter
    {
        public string paramType { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string dataType { get; set; }
        public bool required { get; set; }
        public bool allowMultiple { get; set; }
        public OperationParameterAllowableValues allowableValues { get; set; }
    }

    public class OperationParameterAllowableValues
    {
        public int max { get; set; }
        public int min { get; set; }
        public string valueType { get; set; }
    }

    public class ResourceApiModel
    {
        public string id { get; set; }
        public Dictionary<string, ResourceApiModelProperty> properties { get; set; }
    }
    public class ResourceApiModelProperty
    {
        public string type { get; set; }
        public string description { get; set; }
        public object items { get; set; }
    }
}
