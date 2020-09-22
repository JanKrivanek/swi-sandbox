using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.Contracts.DataCache;
using SolarWinds.SharedCommunication.Contracts.Utils;

namespace SolarWinds.SharedCommunication.DataCache.WCF
{
    internal class DataCacheServiceClient<T> : IDataCache<T> where T : ICacheEntry
    {
        private readonly PollerDataCacheClient _cacheClient = new PollerDataCacheClient();
        //TODO: this must be done differently - different types have different ttl (e.g. topology has longer polling interval)
        // ttl can change during runtime. We might need to expose it through the individual calls
        private readonly TimeSpan _ttl;
        private readonly IAsyncSemaphore _asyncSemaphore;
        private readonly string _key;
        private readonly DataContractSerializer _serializer = new DataContractSerializer(typeof(T));

        public DataCacheServiceClient(string cacheName, TimeSpan ttl, IAsyncSemaphoreFactory semaphoreFactory)
        {
            _ttl = ttl;
            _asyncSemaphore = semaphoreFactory.Create(cacheName + "_MTX");
            _key = cacheName;
        }

        //exception handling is up to client code! - exception should be logged and null data returned to client
        // cache will be in consistent state (just the currently worked on data might or might not be there)
        public async Task<T> GetData(Func<Task<T>> asyncDataFactory, CancellationToken token = default)
        {
            using (await _asyncSemaphore.LockAsync(token))
            {
                SerializedCacheEntry entry = _cacheClient.GetDataCacheEntry(_key, _ttl);

                bool hasData = entry != null;
                T data;
                if (hasData)
                {
                    data = ToData(entry);
                }
                else
                {
                    token.ThrowIfCancellationRequested();
                    data = await asyncDataFactory();
                    entry = FromData(data);
                    _cacheClient.SetDataCacheEntry(_key, _ttl, entry);
                }

                return data;
            }
        }

        private T ToData(SerializedCacheEntry entry)
        {
            if (entry?.SerializedData == null) return default;

            MemoryStream ms = new MemoryStream(entry.SerializedData);

            var ds = new DataContractSerializer(typeof(T));
            return (T)ds.ReadObject(ms);
        }

        private SerializedCacheEntry FromData(T data)
        {
            MemoryStream ms = new MemoryStream();
            _serializer.WriteObject(ms, data);
            byte[] bytes = ms.ToArray();
            return new SerializedCacheEntry(bytes);
        }

        public void Dispose()
        {
            _cacheClient.Close();
            _asyncSemaphore.Dispose();
        }
    }
}