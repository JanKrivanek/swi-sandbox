using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedCommunication.Contracts.RateLimiter;

namespace SharedCommunication.RateLimiter
{
    public class CrossProcessRateLimiterFactory: ICrossProcessRateLimiterFactory
    {
        private readonly IDateTime _dateTime;

        public CrossProcessRateLimiterFactory(IDateTime dateTime)
        {
            _dateTime = dateTime;
        }

        public IRateLimiter OpenOrCreate(string identifier, TimeSpan measureTime, int maxOccurencesPerTime)
        {
            RateLimiterSharedMemoryAccessor accesor =
                new RateLimiterSharedMemoryAccessor(identifier, maxOccurencesPerTime, measureTime.Ticks);
            return new RingMemoryBufferRateLimiter(accesor, _dateTime);
        }

    }
}
