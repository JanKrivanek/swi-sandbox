using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SharedCommunication.DataCache.WCF;
using SharedCommunication.Utils;

namespace SharedMemoryProvider
{
    class Program
    {
        static void Main(string[] args)
        {
            PollerDataCacheService pdc = new PollerDataCacheService(new PlatformDateTime());
            pdc.Start();
            Console.ReadKey();
        }
    }
}
