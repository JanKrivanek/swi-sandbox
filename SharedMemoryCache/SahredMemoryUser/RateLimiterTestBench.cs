using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.Contracts.RateLimiter;
using System.Security.Cryptography;
using SolarWinds.Coding.Utils.Windows.Logger;
using SolarWinds.Logging;
using SolarWinds.SharedCommunication.Utils;

namespace SolarWinds.SharedCommunication.RateLimiter
{

    public class RateLimiterTestBench
    {
        public void RunTest()
        {
            string apiBaseAddress = "https://meraki123/api/v2";
            string apiKey = "15151v2cv1"; //+org id
            string orgId = null;

            string id = new SynchronizationIdentifiersProvider().GetSynchronizationIdentifier(apiBaseAddress, apiKey,
                orgId);

            CrossProcessRateLimiterFactory f = new CrossProcessRateLimiterFactory(new PlatformDateTime(),
                new KernelObjectsPrivilegesChecker(new SolarWindsLogAdapter(this.GetType())));


            int workersCount = 8;
            Parallel.For(1, workersCount + 1, workerId =>
            {
                var limiter = f.OpenOrCreate(id, TimeSpan.FromMilliseconds(1000), 5);
                for (int callId = 0; callId < 20; callId++)
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString("dd-MM-ss.fffffff")} worker [{workerId}] starting call {callId}");
                    //bool canRun = await limiter.WaitTillNextFreeSlotAsync(TimeSpan.FromSeconds(10));
                    bool canRun = limiter.BlockTillNextFreeSlot(TimeSpan.FromSeconds(10));
                    Console.WriteLine($"{DateTime.UtcNow.ToString("dd-MM-ss.fffffff")} worker [{workerId}] finished call {callId}. Success: {canRun}");
                }
            });
        }
    }
}

