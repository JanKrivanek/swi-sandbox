using System;
using Microsoft.Extensions.Caching.Distributed;

namespace SolarWinds.InMemoryCachingUtils
{
    public class ObjectCacheFromDistributed<T> : IObjectCache<T>
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ISerializer<T> _serializer;

        public ObjectCacheFromDistributed(IDistributedCache distributedCache, ISerializer<T> serializer)
        {
            _distributedCache = distributedCache;
            _serializer = serializer;
        }

        public T Get(string key)
        {
            byte[] data = _distributedCache.Get(key);
            return data == null ? default(T) : _serializer.ReadObject(data.ToStream());
        }

        public void Add(string key, T item, DateTimeOffset absoluteExpiration)
        {
            _distributedCache.Set(key, StreamExtensions.ToBytes(stream => _serializer.WriteObject(stream, item)),
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = absoluteExpiration
                });
        }

        public void Remove(string key)
        {
            _distributedCache.Remove(key);
        }

        public void Dispose()
        { }
    }
}
