using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SolarWinds.SharedCommunication.Contracts.DataCache
{
    [DataContract]
    [KnownType(typeof(SerializedCacheEntry))]
    [KnownType(typeof(StringCacheEntry))]
    public abstract class CacheEntryBase : ICacheEntry
    { }

    [DataContract]
    public class SerializedCacheEntry : CacheEntryBase
    {
        public SerializedCacheEntry(byte[] serializedData)
        {
            SerializedData = serializedData;
        }

        [DataMember]
        public byte[] SerializedData { get; private set; }

    }

    [DataContract]
    public class StringCacheEntry : CacheEntryBase
    {
        public StringCacheEntry(string value)
        {
            Value = value;
        }

        [DataMember]
        public string Value { get; private set; }

        public static implicit operator string(StringCacheEntry entry) => entry.Value;
        public static implicit operator StringCacheEntry(string s) => new StringCacheEntry(s);
    }
}
