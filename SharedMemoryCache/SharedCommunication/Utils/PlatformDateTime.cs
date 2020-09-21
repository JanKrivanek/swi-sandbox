using System;
using SharedCommunication.Contracts.Utils;

namespace SharedCommunication.Utils
{
    public class PlatformDateTime : IDateTime
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}