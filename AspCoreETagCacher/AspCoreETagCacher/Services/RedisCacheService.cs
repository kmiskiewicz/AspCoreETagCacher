using System;
using System.Text;
using Jil;
using Microsoft.Extensions.Caching.Distributed;

namespace AspCoreETagCacher.Services
{
    public class RedisCacheService : ICacheRedisService 
    {
        private readonly IDistributedCache _cache;
        private static int DefaultCacheDuration => 60;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public void Store(string key, object content)
        {
            Store(key, content, DefaultCacheDuration);
        }

        public void Store(string key, object content, int duration)
        {
            var s = content as string;
            var toStore = s ?? JSON.Serialize(content);

            duration = duration <= 0 ? DefaultCacheDuration : duration;
            _cache.Set(key, Encoding.UTF8.GetBytes(toStore), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTime.UtcNow + TimeSpan.FromSeconds(duration)
            });
        }

        public T Get<T>(string key) where T : class
        {
            var fromCache = _cache.Get(key);
            if (fromCache == null)
            {
                return null;
            }

            var str = Encoding.UTF8.GetString(fromCache);
            if (typeof(T) == typeof(string))
            {
                return str as T;
            }

            return JSON.Deserialize<T>(str);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
}
