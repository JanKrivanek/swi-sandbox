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

namespace SharedMemoryUser
{

    

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("WcfConsumer test will run on keypress (do not forget to start the server endpoint via SharedMemoryProvider project)");
            Console.ReadKey();

            WcfConsumerCacheTest();

            Console.WriteLine("WcfConsumer test done");
            Console.WriteLine("RateLimiter test will run on keypress");
            Console.ReadKey();


            Console.WriteLine("RateLimiter test done");
            Console.WriteLine("Will exit on keypress");
            Console.ReadKey();

            return;
        }

        static void RateLimiterTest()
        {
            new RateLimiterTestBench().RunTest();
        }

        static void WcfConsumerCacheTest()
        {
            WcfConsumer.Run();
        }
    }

    public class WcfConsumer
    {
        public static async Task Run()
        {
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
