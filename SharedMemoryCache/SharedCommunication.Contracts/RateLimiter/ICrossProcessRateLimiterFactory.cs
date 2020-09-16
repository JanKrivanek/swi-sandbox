using System;
using SharedCommunication.Contracts.RateLimiter;

namespace SharedCommunication.Contracts.RateLimiter
{
    public interface ICrossProcessRateLimiterFactory
    {
        IRateLimiter OpenOrCreate(string identifier, TimeSpan measureTime, int maxOccurencesPerTime);
    }
}