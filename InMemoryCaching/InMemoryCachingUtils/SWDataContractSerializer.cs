using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace SolarWinds.InMemoryCachingUtils
{
    public class SWDataContractSerializer<T>: ISerializer<T>
    {
        private readonly DataContractSerializer _serializer = new DataContractSerializer(typeof(T));

        public void WriteObject(Stream destinationStream, T obj)
        {
            _serializer.WriteObject(destinationStream, obj);
        }

        public T ReadObject(Stream stream)
        {
            Validate(stream);
            stream.Position = 0;
            return (T)_serializer.ReadObject(stream);
        }

        protected virtual void Validate(Stream stream) { }
    }
}
