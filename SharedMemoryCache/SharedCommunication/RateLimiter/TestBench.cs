using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCommunication.Contracts.RateLimiter;

namespace SharedCommunication.RateLimiter
{

    public class TestBench
    {
        public void RunTest()
        {
            string apiKey = "15151v2cv1"; //+org id
            string id = apiKey;//"https://meraki123/api";

            CrossProcessRateLimiterFactory f = new CrossProcessRateLimiterFactory(new PlatformDateTime());


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

