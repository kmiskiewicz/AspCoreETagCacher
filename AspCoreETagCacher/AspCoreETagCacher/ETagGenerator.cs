using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace AspCoreETagCacher
{
    public static class ETagGenerator
    {
        public static string GetETag(HttpRequest req, string body)
        { 
            // TODO: consider supporting VaryBy header in key? (not required atm in this app)
            var combinedKey = req.GetDisplayUrl() + body;
            var combinedBytes = Encoding.UTF8.GetBytes(combinedKey);

            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(combinedBytes);
                var hex = BitConverter.ToString(hash);
                return hex.Replace("-", "");
            }

        }
    }
}
