using System;

namespace SharedCommunication.DataCache
{
    public class DataCacheSettings
    {
        public TimeSpan Ttl { get; set; }
        public string CacheName { get; set; }
    }
}