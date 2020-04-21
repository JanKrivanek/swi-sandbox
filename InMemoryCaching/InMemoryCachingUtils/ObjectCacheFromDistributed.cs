using System;
using Microsoft.Extensions.Caching.Distributed;

namespace SolarWinds.InMemoryCachingUtils
{
    public class ObjectCacheFromDistributed<T> : IObjectCache<T>
    {
        private readonly IDistributedCache _distributedCache;

        public ObjectCacheFromDistributed(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public T Get(string key)
        {
            byte[] data = _distributedCache.Get(key);
            return data == null ? default(T) : SerializationUtils.DeserializeUsingDataContractSerializer<T>(data);
        }

        public void Add(string key, T item, DateTimeOffset absoluteExpiration)
        {
            _distributedCache.Set(key, SerializationUtils.SerializeUsingDataContractSerializer(item),
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
