using System;
using System.ServiceModel;
using SharedCommunication.Contracts.DataCache;

namespace SharedCommunication.DataCache.WCF
{
    [ServiceContract(Name = "PollerDataCache", Namespace = "http://schemas.solarwinds.com/2020/09/jobengine")]
    internal interface IPollerDataCache
    {
        /// <summary>
        /// Fetches the data from remote endpoint - if present and fresh. Otherwise returns null/default
        /// </summary>
        /// <param name="entryKey">the key of the data (usually typename)</param>
        /// <param name="ttl">time to life. If cache contains older item - it gets cleared. Zero/default ttl is used when we want to respect insertion time ttl</param>
        /// <returns></returns>
        [OperationContract]
        SerializedCacheEntry GetDataCacheEntry(string entryKey, TimeSpan ttl = default);

        /// <summary>
        /// Sets the data in cache
        /// Null entry can be used to clear the data
        /// </summary>
        /// <param name="entryKey"></param>
        /// <param name="ttl"></param>
        /// <param name="entry"></param>
        [OperationContract]
        void SetDataCacheEntry(string entryKey, TimeSpan ttl, SerializedCacheEntry entry);
    }
}