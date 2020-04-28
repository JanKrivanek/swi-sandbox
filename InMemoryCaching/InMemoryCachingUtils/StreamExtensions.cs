using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;

namespace SolarWinds.InMemoryCachingUtils
{
    public static class StreamExtensions
    {
        public static byte[] ToBytes(this Action<Stream> streamPopulator)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                streamPopulator(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static Stream ToStream(this byte[] bytes)
        {
            return new MemoryStream(bytes);
        }
    }

    
}
