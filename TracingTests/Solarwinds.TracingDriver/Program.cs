using System;
using System.Threading;
using Solarwinds.TracingWrapper;

namespace Solarwinds.TracingDriver
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test starting");
            TraceTest();
            Console.WriteLine("Done");
        }

        private static void TraceTest()
        {
            TracingWrapper.TracingHelper.WaitTillReady(TimeSpan.FromSeconds(3));

            TracingWrapper.TracingHelper.StartTrace("Trace_A");

            using (var span = TracingHelper.BeginQuerySpan("AnotherQuerySpan2", "SELECT * FROM FOOBAR", "ClickHouse", "ClickhouseDocker"))
            {
                //query running ...
                Thread.Sleep(100);
            }

            using (var span = TracingHelper.BeginCacheSpan("CacheSpan", "insert", "memcached", "cache-host"))
            {
                //cache access running ...
                Thread.Sleep(100);
            }

            using (var span = TracingHelper.BeginRpcSpan("RpcSpan01", "wcf", "insertFoo", "rpc-host"))
            {
                //query running ...
                Thread.Sleep(100);
            }


            TracingWrapper.TracingHelper.EndTrace("Trace_A");
        }
    }
}
