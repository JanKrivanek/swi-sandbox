using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SolarWinds.InMemoryCachingUtils
{
    public interface ISerializer<T>
    {
        void WriteObject(Stream destinationStream, T obj);
        T ReadObject(Stream stream);
    }
}
