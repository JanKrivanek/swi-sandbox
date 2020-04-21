using System;
using Microsoft.Extensions.Caching.Memory;

namespace SolarWinds.InMemoryCachingUtils
{
    public class ObjectCacheFromMemoryCache<T> : IObjectCache<T>
    {
        private IMemoryCache _cache;

        public ObjectCacheFromMemoryCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public T Get(string key)
        {
            return (T)_cache.Get(key);
        }

        public void Add(string key, T item, DateTimeOffset absoluteExpiration)
        {
            using (var entry = _cache.CreateEntry(key))
            {
                entry.SetValue(item);
                entry.AbsoluteExpiration = absoluteExpiration;
            }
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }
    }
}