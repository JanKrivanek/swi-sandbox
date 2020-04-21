using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SolarWinds.InMemoryCachingUtils
{
    public class ObjectCacheFromDistributedWithFallbackToLocal<T> : IObjectCache<T>
    {
        private readonly IDistributedCache _distributedCache;
        //Cannot easily DI nongeneric ILogger with Microsoft.Extensions.DependencyInjection
        // https://stackoverflow.com/questions/51345161/should-i-take-ilogger-iloggert-iloggerfactory-or-iloggerprovider-for-a-libra
        private readonly ILogger<ObjectCacheFromDistributedWithFallbackToLocal<T>> _logger;
        private IObjectCache<T> _localCache;

        public ObjectCacheFromDistributedWithFallbackToLocal(
            IDistributedCache distributedCache,
            ILogger<ObjectCacheFromDistributedWithFallbackToLocal<T>> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public T Get(string key)
        {
            byte[] data;
            try
            {
                data = _distributedCache.Get(key);
                DiscardLocalCacheIfNeeded();
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, "Error fetching data for key: {key}", key);

                var localCache = _localCache;
                return localCache == null ? default(T) : (T)localCache.Get(key);
            }

            return data == null ? default(T) : SerializationUtils.DeserializeUsingDataContractSerializer<T>(data);
        }

        public void Add(string key, T item, DateTimeOffset absoluteExpiration)
        {
            try
            {
                _distributedCache.Set(key, SerializationUtils.SerializeUsingDataContractSerializer(item),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = absoluteExpiration
                    });
                DiscardLocalCacheIfNeeded();
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, "Error adding data for key: {key}", key);

                var localCache = _localCache;
                if (localCache == null)
                {
                    //We could DI this, but would lead to convoluted resolution of IObjectCache service
                    //  we know that here we want to fall back to local memory
                    localCache = new ObjectCacheFromMemoryCache<T>(new MemoryCache(new MemoryCacheOptions()));
                    _localCache = localCache;
                }

                localCache.Add(key, item, absoluteExpiration);
            }
        }

        public void Remove(string key)
        {
            try
            {
                _distributedCache.Remove(key);
                DiscardLocalCacheIfNeeded();
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, "Error removing data for key: {key}", key);

                _localCache?.Remove(key);
            }
        }

        private void DiscardLocalCacheIfNeeded()
        {
            _localCache?.Dispose();
            _localCache = null;
        }

        public void Dispose()
        {
            _localCache?.Dispose();
        }
    }
}