using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCommunication.Contracts.DataCache;
using SharedCommunication.Contracts.Utils;
using SharedCommunication.DataCache.WCF;
using SharedCommunication.RateLimiter;
using SharedCommunication.Utils;
using SolarWinds.Coding.Utils.Logger;
using SolarWinds.Coding.Utils.Windows.Logger;

namespace SahredMemoryUser
{

    

    class Program
    {
        static async Task Main(string[] args)
        {
            WcfConsumer.Run();


            Console.ReadKey();

            var logger = new SolarWindsLogAdapter(typeof(Program)); //new DevNullLogAdapter()
            AsyncSemaphoreFactory factory = new AsyncSemaphoreFactory(logger);

            IAsyncSemaphore a = factory.Create(@"Global\BB");
            a.WaitAsync().Wait();
            a.Release();

            {
                IAsyncSemaphore b = factory.Create("BB");
                a.WaitAsync().Wait();
            }

            CancellationTokenSource cts = new CancellationTokenSource(5000);
            a.WaitAsync().Wait(cts.Token);

            a.Dispose();

            //TestBench tb = new TestBench();
            //tb.RunTest();

            //new MMFTests().Run();

            //new TestRate().RunTest();

            //new Test().Run();
            //new Test().Run();

            
        }
    }

    public class WcfConsumer
    {
        public static async Task Run()
        {
            //PollerDataCacheClient cl = new PollerDataCacheClient();
            //string key = "fff";
            //var v1 = cl.GetDataCacheEntry(key, TimeSpan.MaxValue);

            //cl.SetDataCacheEntry(key, TimeSpan.FromSeconds(50), new SerializedCacheEntry(new byte[]{2,4,6}));
            //var v2 = cl.GetDataCacheEntry(key);

            //cl.SetDataCacheEntry(key, TimeSpan.Zero, null);

            //var v3 = cl.GetDataCacheEntry(key);

            try
            {
                DataCacheServiceClientFactory<StringCacheEntry> fac =
                    new DataCacheServiceClientFactory<StringCacheEntry>(
                        new AsyncSemaphoreFactory(new SolarWindsLogAdapter(typeof(Program))));

                var cache = fac.CreateCache("HwH_meraki.com_apikey_orgKey", TimeSpan.FromMinutes(5));

                var res1 = await cache.GetData(() => Task.FromResult((StringCacheEntry)"fdfdfdfd"));

                var res2 = await cache.GetData(() => Task.FromResult((StringCacheEntry)"ggfgfgfgfgf"));

                await Task.Delay(TimeSpan.FromSeconds(2));

                var res3 = await cache.GetData(() => Task.FromResult((StringCacheEntry)"123456"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
    }

}
