using System;
using System.Runtime.Serialization;

namespace SolarWinds.InMemoryCachingUtils
{
    [Serializable]
    public class XmlValidationException : Exception
    {
        public XmlValidationException() { }
        public XmlValidationException(string message) : base(message) { }
        public XmlValidationException(string message, Exception inner) : base(message, inner) { }
        protected XmlValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}