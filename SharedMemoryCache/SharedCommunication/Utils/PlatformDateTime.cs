using System;
using SharedCommunication.Contracts.Utils;

namespace SharedCommunication.Utils
{
    internal class PlatformDateTime : IDateTime
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}