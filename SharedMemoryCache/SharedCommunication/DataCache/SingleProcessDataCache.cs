using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.Contracts.DataCache;
using SolarWinds.SharedCommunication.Contracts.Utils;

namespace SolarWinds.SharedCommunication.DataCache
{
    public class SingleProcessDataCache<T> : IDataCache<T> where T : ICacheEntry
    {
        //SemaphoreSlim cannot be created from handle - so we need to make sure to create single
        private static ConcurrentDictionary<string, IDataCache<T>> _instances = new ConcurrentDictionary<string, IDataCache<T>>();
        private readonly SemaphoreSlim _sp;
        private readonly TimeSpan _ttl;
        private readonly IDateTime _dateTime;

        private T _data;
        private DateTime _lastChangedUtc;

        public static IDataCache<T> Create(TimeSpan ttl, IDateTime dateTime)
        {
            return _instances.GetOrAdd(typeof(T).Name, name => new SingleProcessDataCache<T>(ttl, dateTime));
        }

        public static IDataCache<T> Create(string cacheName, TimeSpan ttl, IDateTime dateTime)
        {
            return _instances.GetOrAdd(cacheName, name => new SingleProcessDataCache<T>(ttl, dateTime));
        }

        public static IDataCache<T> Create(DataCacheSettings settings, IDateTime dateTime)
        {
            return Create(settings.CacheName, settings.Ttl, dateTime);
        }

        private SingleProcessDataCache(TimeSpan ttl, IDateTime dateTime)
        {
            _sp = new SemaphoreSlim(1,1);
            _ttl = ttl;
            _dateTime = dateTime;
        }

        public async Task<T> GetData(Func<Task<T>> asyncDataFactory, CancellationToken token = default)
        {
            await _sp.WaitAsync(token);
            //on cancellation exception would be thrown and we won't get here
            try
            {
                bool hasData = _lastChangedUtc >= _dateTime.UtcNow - _ttl;
                if (!hasData)
                {
                    _data = await asyncDataFactory();
                    _lastChangedUtc = _dateTime.UtcNow;
                }

                return _data;
            }
            finally
            {
                _sp.Release();
            }
        }

        public void Dispose()
        {
            _sp?.Dispose();
        }
    }
}
