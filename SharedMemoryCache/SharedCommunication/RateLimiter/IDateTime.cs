using System;

namespace SharedCommunication.RateLimiter
{
    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }
}