using System;
using System.IO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SolarWinds.InMemoryCachingUtils
{
    //In a real use case scenario there would need to be more involved logic around deciding when to fallback
    // to and from local cache
    public sealed class ObjectCacheFromDistributedWithFallbackToLocal<T> : IObjectCache<T>
    {
        private readonly IDistributedCache _distributedCache;
        //Cannot easily DI nongeneric ILogger with Microsoft.Extensions.DependencyInjection
        // https://stackoverflow.com/questions/51345161/should-i-take-ilogger-iloggert-iloggerfactory-or-iloggerprovider-for-a-libra
        private readonly ILogger<ObjectCacheFromDistributedWithFallbackToLocal<T>> _logger;
        private readonly ISerializer<T> _serializer;
        private IObjectCache<T> _localCache;

        public ObjectCacheFromDistributedWithFallbackToLocal(
            IDistributedCache distributedCache,
            ILogger<ObjectCacheFromDistributedWithFallbackToLocal<T>> logger,
            ISerializer<T> serializer)
        {
            _distributedCache = distributedCache;
            _logger = logger;
            _serializer = serializer;
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

            return data == null ? default(T) : _serializer.ReadObject(data.ToStream());
        }

        public void Add(string key, T item, DateTimeOffset absoluteExpiration)
        {

            byte[] data = StreamExtensions.ToBytes(stream => _serializer.WriteObject(stream, item));

            try
            {
                _distributedCache.Set(key, data,
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