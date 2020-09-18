using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SharedCommunication.Contracts.DataCache;
using SharedCommunication.Contracts.Utils;

namespace SharedCommunication.DataCache
{
    public class SharedMemoryDataCache<T> : IDisposable, IDataCache<T> where T : ICacheEntry
    {
        //Semaphore and memory segments are named - so we are fine recreating them in a same process
        private readonly IAsyncSemaphore _asyncSemaphore;
        private readonly ISharedMemorySegment _memorySegment;
        private readonly TimeSpan _ttl;
        private readonly IDateTime _dateTime;

        public SharedMemoryDataCache(DataCacheSettings settings, IDateTime dateTime,
            IAsyncSemaphoreFactory semaphoreFactory)
            : this(settings.CacheName, settings.Ttl, dateTime, semaphoreFactory)
        {  }

        public SharedMemoryDataCache(string cacheName, TimeSpan ttl, IDateTime dateTime, IAsyncSemaphoreFactory semaphoreFactory)
        {
            //TODO: to be added to run properly accross sessions
            //cacheName = @"Global\" + cacheName;

            _asyncSemaphore = semaphoreFactory.Create(cacheName + "_MTX");
            _memorySegment = new SharedMemorySegment(cacheName + "_MMF");
            _ttl = ttl;
            _dateTime = dateTime;
        }

        public async Task<T> GetData(Func<Task<T>> asyncDataFactory, CancellationToken token = default)
        {

            using (await _asyncSemaphore.LockAsync(token))
            {
                bool hasData = _memorySegment.LastChangedUtc >= _dateTime.UtcNow - _ttl;
                T data;
                if (hasData)
                {
                    data = _memorySegment.ReadData<T>();
                }
                else
                {
                    data = await asyncDataFactory();
                    _memorySegment.WriteData(data);
                }

                return data;
            }
        }

        public void Dispose()
        {
            _asyncSemaphore?.Dispose();
        }
    }
}