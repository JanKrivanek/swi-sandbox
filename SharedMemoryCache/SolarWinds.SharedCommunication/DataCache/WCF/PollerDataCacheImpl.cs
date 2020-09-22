using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using SolarWinds.SharedCommunication.Contracts.DataCache;
using SolarWinds.SharedCommunication.Contracts.Utils;
using SolarWinds.Coding.Utils.Logger;

namespace SolarWinds.SharedCommunication.DataCache.WCF
{
    public class PollerDataCacheImpl : IPollerDataCache
    {
        private class InternalEntry
        {
            public InternalEntry(SerializedCacheEntry data, TimeSpan ttl, DateTime insertedUtc)
            {
                Data = data;
                Ttl = ttl;
                InsertedUtc = insertedUtc;
            }

            public SerializedCacheEntry Data { get; private set; }
            public TimeSpan Ttl { get; private set; }
            public DateTime InsertedUtc { get; private set; }

            public TimeSpan RemainingTtl(DateTime utcNow, TimeSpan explicitTtl = default)
            {
                return (explicitTtl != TimeSpan.Zero ? explicitTtl : Ttl) - (utcNow - InsertedUtc);
            }
        }

        private readonly ConcurrentDictionary<string, InternalEntry> _cache = new ConcurrentDictionary<string, InternalEntry>();
        private readonly IDateTime _dateTime;

        public PollerDataCacheImpl(IDateTime dateTime)
        {
            _dateTime = dateTime;
        }

        public SerializedCacheEntry GetDataCacheEntry(string entryKey, TimeSpan ttl = default)
        {
            InternalEntry entry;
            if (_cache.TryGetValue(entryKey, out entry) && entry.RemainingTtl(_dateTime.UtcNow, ttl) < TimeSpan.Zero)
            {
                entry = null;
            }

            return entry?.Data;
        }

        public void SetDataCacheEntry(string entryKey, TimeSpan ttl, SerializedCacheEntry entry)
        {
            InternalEntry ie = new InternalEntry(entry, ttl, _dateTime.UtcNow);
            _cache[entryKey] = ie;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public long GetCacheSize()
        {
            return _cache.Values.Sum(v => v.Data.SerializedData.Length);
        }

        /// <summary>
        /// Purges stale and soon stale items (if maximum size is requested)
        /// It doesn't perform locking so purging doesn't happen on snapshot in time - rather on life data (but it's safe to call in concurrent environment)
        /// </summary>
        /// <param name="maxCacheSizeInBytes"></param>
        public void RunPurge(long? maxCacheSizeInBytes = default)
        {
            long bytesToDelete = 0;
            if (maxCacheSizeInBytes.HasValue)
            {
                if(maxCacheSizeInBytes <= 0)
                    throw new ArgumentException("maxCacheSizeInBytes is expected to be greater than zero. Otherwise you can just clear the cache");
                bytesToDelete = GetCacheSize() - maxCacheSizeInBytes.Value;
            }

            DateTime nowUtc = _dateTime.UtcNow;
            foreach (KeyValuePair<string, InternalEntry> keyValuePair in _cache.OrderBy(kp => kp.Value.RemainingTtl(nowUtc)))
            {
                if (bytesToDelete > 0 || keyValuePair.Value.RemainingTtl(nowUtc) < TimeSpan.Zero)
                {
                    if (_cache.TryRemove(keyValuePair.Key, out _))
                    {
                        bytesToDelete -= keyValuePair.Value.Data.SerializedData.Length;
                    }
                }
                else
                {
                    return;
                }
            }
        }
    }
}
