using System;

namespace SharedCommunication.RateLimiter
{
    internal class PlatformDateTime : IDateTime
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}