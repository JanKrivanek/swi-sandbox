using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.Contracts.RateLimiter;
using SolarWinds.SharedCommunication.Contracts.Utils;
using SolarWinds.SharedCommunication.Utils;

namespace SolarWinds.SharedCommunication.RateLimiter
{
    public class CrossProcessRateLimiterFactory: ICrossProcessRateLimiterFactory
    {
        private readonly IDateTime _dateTime;
        private readonly IKernelObjectsPrivilegesChecker _privilegesChecker;

        public CrossProcessRateLimiterFactory(IDateTime dateTime, IKernelObjectsPrivilegesChecker privilegesChecker)
        {
            _dateTime = dateTime;
            _privilegesChecker = privilegesChecker;
        }

        public IRateLimiter OpenOrCreate(string identifier, TimeSpan measureTime, int maxOccurencesPerTime)
        {
            RateLimiterSharedMemoryAccessor accesor =
                new RateLimiterSharedMemoryAccessor(identifier, maxOccurencesPerTime, measureTime.Ticks, _privilegesChecker);
            return new RingMemoryBufferRateLimiter(accesor, _dateTime);
        }

    }
}
