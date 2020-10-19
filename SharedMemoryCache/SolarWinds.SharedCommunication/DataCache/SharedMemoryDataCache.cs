using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.Contracts.DataCache;
using SolarWinds.SharedCommunication.Contracts.Utils;
using SolarWinds.SharedCommunication.Utils;

namespace SolarWinds.SharedCommunication.DataCache
{
    public class SharedMemoryDataCache<T> : IDataCache<T> where T : ICacheEntry
    {
        //Semaphore and memory segments are named - so we are fine recreating them in a same process
        private readonly IAsyncSemaphore _asyncSemaphore;
        private readonly ISharedMemorySegment _memorySegment;
        private readonly TimeSpan _ttl;
        private readonly IDateTime _dateTime;

        public SharedMemoryDataCache(
            DataCacheSettings settings, 
            IDateTime dateTime,
            IAsyncSemaphoreFactory semaphoreFactory,
            IKernelObjectsPrivilegesChecker kernelObjectsPrivilegesChecker)
            : this(settings.CacheName, settings.Ttl, dateTime, semaphoreFactory, kernelObjectsPrivilegesChecker)
        {  }

        public SharedMemoryDataCache(
            string cacheName, 
            TimeSpan ttl, 
            IDateTime dateTime, 
            IAsyncSemaphoreFactory semaphoreFactory,
            IKernelObjectsPrivilegesChecker kernelObjectsPrivilegesChecker)
        {
            _asyncSemaphore = semaphoreFactory.Create(cacheName + "_MTX", kernelObjectsPrivilegesChecker);
            _memorySegment = new SharedMemorySegment(cacheName + "_MMF", kernelObjectsPrivilegesChecker);
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
            _memorySegment.Dispose();
            _asyncSemaphore.Dispose();
        }
    }
}