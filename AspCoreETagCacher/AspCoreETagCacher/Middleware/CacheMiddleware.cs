using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace AspCoreETagCacher.Middleware
{

    /// <summary>
    ///Purpose of CacheETagMiddleware is to intercept request, 
    ///and provide custom stream to which further middlewares could write (and read).
    ///When the middleware invocation process will return to CacheMiddleware,
    ///this stream's content will be copied to original response stream. 
    ///</summary>
    public class CacheETagMiddleware
    {
        protected RequestDelegate NextMiddleware;

        public CacheETagMiddleware(RequestDelegate nextMiddleware)
        {
            NextMiddleware = nextMiddleware;
        }

        public async Task Invoke(HttpContext httpContext)
        {

            var resp = httpContext.Response;
            var req = httpContext.Request;


            using (var buffer = new MemoryStream())
            {
                // populate a stream with the current response data
                var stream = resp.Body;
                // setup response.body to point at our buffer
                resp.Body = buffer;

                try
                {
                    // call controller/middleware actions etc. to populate the response body 
                    await NextMiddleware.Invoke(httpContext);
                }
                catch
                {
                    // controller/ or other middleware threw an exception, copy back and rethrow
                    buffer.CopyTo(stream);
                    resp.Body = stream;
                    // looks weird, but required to keep the stream writable in edge cases like exceptions in other middleware
                    throw;
                }
                if (resp.StatusCode == StatusCodes.Status204NoContent)
                {
                    return;
                }
                buffer.Position = 0;
                if (resp.Headers[HeaderNames.ETag].Count == 0)
                {
                    CalculateETagAndCopyStreamIfNecessary(resp, req, buffer, stream);
                }
                else
                {
                    CopyStreamIfNecessary(resp, buffer, stream);
                }

            }
        }
        //http://stackoverflow.com/questions/135020/advantages-to-using-private-static-methods
        //static to improve performance
        private static void CopyStreamIfNecessary(HttpResponse resp, MemoryStream buffer, Stream stream)
        {
            // copy buffer back to response.body
            // copying begins at the current position in the stream (which we populated earlier)
            // note that if Status 304 header, body will be blanked out as per http spec
            if (resp.StatusCode == StatusCodes.Status304NotModified) return;
            // reset the buffer and read out the contents
            buffer.Position = 0;
            buffer.CopyTo(stream);
            resp.Body = stream;
            // looks weird, but required to keep the stream writable in edge cases like exceptions in other middleware
        }
        //http://stackoverflow.com/questions/135020/advantages-to-using-private-static-methods
        //static to improve performance
        private static void CalculateETagAndCopyStreamIfNecessary(HttpResponse resp, HttpRequest req, MemoryStream buffer, Stream stream)
        {
            using (var reader = new StreamReader(buffer))
            {
                if (resp.Headers[HeaderNames.ETag].Count == 0)
                {
                    var body = reader.ReadToEnd();
                    resp.Headers.Add(HeaderNames.ETag, ETagGenerator.GetETag(req, body));
                    if (req.Headers[HeaderNames.IfNoneMatch].Count != 0 && req.Headers[HeaderNames.IfNoneMatch] == resp.Headers[HeaderNames.ETag])
                    {
                        resp.StatusCode = StatusCodes.Status304NotModified; // this will blank response
                    }
                }
                CopyStreamIfNecessary(resp, buffer, stream);

            }
        }
    }
}
