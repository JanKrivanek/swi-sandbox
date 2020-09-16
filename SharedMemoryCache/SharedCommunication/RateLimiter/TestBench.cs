using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharedCommunication.RateLimiter
{

    public class TestBench
    {
        public void RunTest()
        {
            string id = "testRateLimiter";

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

            //RateLimiterSharedMemoryAccessor ma = new RateLimiterSharedMemoryAccessor("fdffd", 4, 20);

            //bool enter1 = ma.TryEnterSynchronizedRegion();
            //bool enter2 = ma.TryEnterSynchronizedRegion();

            //for (int i = 10; i < 20; i++)
            //{
            //    long o = ma.OldestTimestampTicks;
            //    long c = ma.CurrentTimestampTicks;
            //    ma.CurrentTimestampTicks = i;
            //}

            //ma.ExitSynchronizedRegion();

            //bool enter3 = ma.TryEnterSynchronizedRegion();
            //bool enter4 = ma.TryEnterSynchronizedRegion();
        }
    }
}

