using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.AccessControl;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.DataCache.WCF;
using SolarWinds.SharedCommunication.Utils;

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
