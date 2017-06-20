using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AspCoreETagCacher.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using NuGet.Protocol;

namespace AspCoreETagCacher.Attribiutes
{

    [AttributeUsage(AttributeTargets.Method)]
    public class RedisEtagCacheAttribute : CacheAttribute
    {
        public RedisEtagCacheAttribute()
        {
            CacheType = CacheType.Redis;
            CacheLocation = CacheLocation.Any;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class MemoryEtagCacheAttribute : CacheAttribute
    {
        public MemoryEtagCacheAttribute()
        {
            CacheType = CacheType.Memory;
            CacheLocation = CacheLocation.Any;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ResponseEtagCacheAttribute : CacheAttribute
    {
        public new int ServerSideDuration { private set; get; }

        public ResponseEtagCacheAttribute()
        {
            CacheType = CacheType.None;
            CacheLocation = CacheLocation.Client;
        }
    }
    public enum CacheType { Redis, Memory, None }

    public enum CacheLocation
    {
        Any,
        Client,
        Server
    }


    [AttributeUsage(AttributeTargets.Method)]
    public abstract class CacheAttribute : ResultFilterAttribute, IActionFilter, IResourceFilter
    {
        protected ICacheService CacheService { set; get; }
        protected CacheType CacheType { set;  get; }
        protected CacheLocation CacheLocation {  get; set; } = CacheLocation.Any;

        /// <summary>
        /// In seconds
        /// </summary>
        public int ClientSideDuration { set; get; } = 60;

        /// <summary>
        /// In seconds
        /// </summary>
        public int ServerSideDuration { set; get; } = 30;

        private string _cacheKey;
        private const string ContentType = "_contentType";


        #region private methods
        /// <summary>
        /// only get methods are supported 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        protected bool ValidateMethod(HttpRequest req, HttpResponse resp)
        {
            return req.Method == "GET" && resp.StatusCode == StatusCodes.Status200OK;
        }
        protected void GetCachingService(FilterContext context)
        {

            switch (CacheType)
            {
                case CacheType.Memory:
                    {
                        CacheService =
                   context.HttpContext.RequestServices.GetService(typeof(ICacheMemoryService)) as ICacheService;

                        break;
                    }
                case CacheType.Redis:
                    {
                        CacheService =
                                    context.HttpContext.RequestServices.GetService(typeof(ICacheRedisService)) as ICacheService;
                        break;
                    }

            }
        }

        protected void SetCacheHeaderForClientSideCaching(HttpResponse response, string eTag)
        {

            if (response.Headers[HeaderNames.ETag].Count != 0)
            {
                response.Headers.Remove(HeaderNames.ETag);
            }
            response.Headers.Add(HeaderNames.ETag, new[] { eTag });

            // check cache-control not already set - so that controller actions can override caching 
            // behaviour with [ResponseCache] attribute
            // (also see StaticFileOptions)
            var cc = response.GetTypedHeaders().CacheControl;
            if ((cc != null && (cc.NoCache || cc.NoStore)) || CacheLocation == CacheLocation.Server)
                return;

            response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(ClientSideDuration), // for client 
                SharedMaxAge = TimeSpan.FromSeconds(ClientSideDuration) // for caching proxy
            };
        }

        #endregion
        #region pipeline methods 
        /// <summary>
        /// If the result is cached stop the pipeline and return the result
        /// </summary>
        /// <remarks>
        /// //c ActionExecutedContext.Canceled will be set to true if the action execution was short-circuited by another filter.
        /// </remarks>
        /// <param name="context"></param>
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (CacheLocation == CacheLocation.Client)
            {
                return;
            }

            var req = context.HttpContext.Request;
            var resp = context.HttpContext.Response;
            _cacheKey = $"{req.Path}{ req.QueryString}";
            GetCachingService(context);
            //get cached results
            var cachedResult = CacheService.Get<string>(_cacheKey);
            var contentType = CacheService.Get<string>($"{_cacheKey}{ContentType}");
            var eTag = CacheService.Get<string>($"{_cacheKey}_{HeaderNames.ETag}");

            //if we don't have cached output than return and execute the method
            //data will be store in the the cache later in the method OnResultExecuted
            if (cachedResult == null || string.IsNullOrEmpty(contentType) || string.IsNullOrEmpty(eTag))
            {
                return;
            }
            SetCacheHeaderForClientSideCaching(context.HttpContext.Response, eTag); //refresh cache lifetime on client side 

            //we have to check the eTag, if the value is the same (which means that client already have the current version)
            if (req.Headers[HeaderNames.IfNoneMatch].Count != 0 && req.Headers[HeaderNames.IfNoneMatch] == context.HttpContext.Response.Headers[HeaderNames.ETag])
            {
                // https://tools.ietf.org/html/rfc7232#section-4.1
                // The server generating a 304 response MUST generate any of the following header 
                // fields that would have been sent in a 200(OK) response to the same 
                // request: Cache-Control, Content-Location, Date, ETag, Expires, and Vary.
                context.Result = new EmptyResult();
                context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified; // this will blank response
                SetCacheHeaderForClientSideCaching(context.HttpContext.Response, eTag); //refresh cache lifetime on client side 
                return;
            }
            context.Result = new ContentResult();
            resp.ContentType = contentType;
            resp.StatusCode = StatusCodes.Status200OK;//We add to cache only results with StatusCodes.Status200OK 

            var responseStream = resp.Body;
            responseStream.Position = 0;
            if (responseStream.Length <= cachedResult.Length)
            {
                responseStream.SetLength(cachedResult.Length);
            }
            using (var writer = new StreamWriter(responseStream, Encoding.UTF8, 4096, true))
            {
                writer.Write(cachedResult);
                writer.Flush();
                responseStream.Flush();
            }
            context.HttpContext.Response.Headers.Add("CachingMechanism", CacheType.ToString());
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        /// <summary>
        /// In this method you can set up additional headers (actually is the last moment to do this), the next method in the pipline is OnResultExecuting where the stream is closed for writing 
        ///  In the method we set up eTag header and cache attributs for client side (which do simmilar thing as a native ResponseCache but with additional ETags)
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var req = context.HttpContext.Request;
            var resp = context.HttpContext.Response;
            if (!ValidateMethod(req, resp))
            {
                return;
            }

            var eTag = ETagGenerator.GetETag(context.HttpContext.Request, context.Result?.ToJson());
            SetCacheHeaderForClientSideCaching(resp, eTag);
            if (CacheLocation == CacheLocation.Client)
            {
                return;
            }
            Task.Factory.StartNew(() =>
            {
                CacheService.Store($"{_cacheKey}_{HeaderNames.ETag}", eTag, ServerSideDuration);
            });
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
        }

        /// <summary>
        /// In the method we copy the response from the body to a cache, if the client version (based on eTag) 
        /// is still up to date we return status code 304 and later in the CacheMiddleware we skip assiging the result of method to the response body  
        /// </summary>
        /// <param name="context"></param>
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            var req = context.HttpContext.Request;
            var resp = context.HttpContext.Response;

            if (!ValidateMethod(req, resp))
            {
                return;
            }

            if (CacheLocation != CacheLocation.Client)
            {
                var responseStream = resp.Body;
                responseStream.Position = 0;
                using (var streamReader = new StreamReader(responseStream, Encoding.UTF8, true, 512, true))
                {
                    var toCache = streamReader.ReadToEnd();
                    var contentType = resp.ContentType;
                    Task.Factory.StartNew(() =>
                    {
                        CacheService.Store($"{_cacheKey}{ContentType}", contentType, ServerSideDuration);
                        CacheService.Store(_cacheKey, toCache, ServerSideDuration);
                    });
                }
            }
            //if after method excecution and calculation of the eTag we recieve the same hastag we set up 304 status code
            if (req.Headers[HeaderNames.IfNoneMatch].Count != 0 && req.Headers[HeaderNames.IfNoneMatch] == context.HttpContext.Response.Headers[HeaderNames.ETag])
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified; // this will blank response
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        #endregion
    }
}

