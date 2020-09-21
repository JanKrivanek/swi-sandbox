using System;
using System.ServiceModel;
using SharedCommunication.Contracts.DataCache;

namespace SharedCommunication.DataCache.WCF
{
    internal class PollerDataCacheClient : ClientBase<IPollerDataCache>, IPollerDataCache
    {
        public PollerDataCacheClient()
        {

        }

        public void SetDataCacheEntry(string entryKey, TimeSpan ttl, SerializedCacheEntry entry)
        {
            Channel.SetDataCacheEntry(entryKey, ttl, entry);
        }

        public SerializedCacheEntry GetDataCacheEntry(string entryKey, TimeSpan ttl = default)
        {
            return Channel.GetDataCacheEntry(entryKey, ttl);
        }
    }
}