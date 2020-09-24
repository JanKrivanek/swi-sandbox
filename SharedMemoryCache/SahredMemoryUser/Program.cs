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
using SolarWinds.SharedCommunication.Contracts.DataCache;
using SolarWinds.SharedCommunication.Contracts.Utils;
using SolarWinds.SharedCommunication.DataCache.WCF;
using SolarWinds.SharedCommunication.RateLimiter;
using SolarWinds.SharedCommunication.Utils;
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
            Console.WriteLine("RETURN");
            return;

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
                DataCacheServiceClientFactory<string> fac =
                    new DataCacheServiceClientFactory<string>(
                        new AsyncSemaphoreFactory(new SolarWindsLogAdapter(typeof(Program))));

                var cache = fac.CreateCache("HwH_meraki.com_apikey_orgKey", TimeSpan.FromMinutes(5));

                var res1 = await cache.GetData(() => Task.FromResult((string)"fdfdfdfd"));
                Console.WriteLine("res1:" + res1);

                var res2 = await cache.GetData(() => Task.FromResult((string)"ggfgfgfgfgf"));
                Console.WriteLine("res2:" + res2);

                await Task.Delay(TimeSpan.FromSeconds(2));

                var res3 = await cache.GetData(() => Task.FromResult((string)"123456"));
                Console.WriteLine("res3:" + res3);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
    }

}
