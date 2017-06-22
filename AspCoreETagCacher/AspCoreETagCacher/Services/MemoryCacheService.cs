using System;
using Microsoft.Extensions.Caching.Memory;

namespace AspCoreETagCacher.Services
{
    public class MemoryCacheService : ICacheMemoryService 
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void Store(string key, object content)
        {
            Store(key, content, DefaultCacheDuration);
        }

        public void Store(string key, object content, int duration)
        {
            object cached;
            if (_cache.TryGetValue(key, out cached))
            {
                _cache.Remove(key);
            }

            _cache.Set(key, content,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.UtcNow + TimeSpan.FromSeconds(duration),
                    Priority = CacheItemPriority.Low
                });
        }


        private static int DefaultCacheDuration => 60;

        public T Get<T>(string key) where T : class
        {
            object result;
            if (_cache.TryGetValue(key, out result))
            {
                return result as T;
            }
            return null;
        }

        public void Remove(string key)
        {
             _cache.Remove(key);
        }
    }
}
