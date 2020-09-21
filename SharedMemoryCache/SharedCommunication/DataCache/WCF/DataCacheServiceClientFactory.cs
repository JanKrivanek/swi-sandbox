using System;
using SharedCommunication.Contracts.DataCache;
using SharedCommunication.Contracts.Utils;

namespace SharedCommunication.DataCache.WCF
{
    public class DataCacheServiceClientFactory<T> where T : CacheEntryBase
    {
        private readonly PollerDataCacheClient _cacheClient = new PollerDataCacheClient();
        private readonly IAsyncSemaphoreFactory _semaphoreFactory;

        public DataCacheServiceClientFactory(IAsyncSemaphoreFactory semaphoreFactory)
        {
            _semaphoreFactory = semaphoreFactory;
        }

        //optionaly we can change ttl to be per cache call
        public IDataCache<T> CreateCache(string cacheName, TimeSpan ttl)
        {
            var asyncSemaphore = _semaphoreFactory.Create(cacheName + "_MTX");
            return new DataCacheServiceClient<T>(cacheName, ttl, _semaphoreFactory);
        }
    }
}