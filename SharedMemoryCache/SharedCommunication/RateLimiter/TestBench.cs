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
using SolarWinds.SharedCommunication.Utils;

namespace SolarWinds.SharedCommunication.RateLimiter
{

    public class TestBench
    {
        public void RunTest()
        {
            string apiBaseAddress = "https://meraki123/api/v2";
            string apiKey = "15151v2cv1"; //+org id
            string orgId = null;
            string uniqueIdentity = apiBaseAddress + "_" + apiKey + (orgId == null ? null : ("_" + orgId));
            //it's better to randomize salt; on the other hand we must get consistent result across processes
            // so some common schema must be used. No salting might be acceptable as well - the identity
            // should be long and random enough to prevent against hashed dictionary attack.
            string salt = "xyzabc";

            //now hash to prevent info leaking
            var hashAlgo = new SHA256Managed();
            var hash = hashAlgo.ComputeHash(Encoding.UTF8.GetBytes(uniqueIdentity + salt));
            //this can now be used as identity of shared handles (memory mapped files etc.)
            string id = Convert.ToBase64String(hash);


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

