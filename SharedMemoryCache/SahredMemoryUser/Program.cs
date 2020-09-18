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
using SharedCommunication.Contracts.Utils;
using SharedCommunication.RateLimiter;
using SharedCommunication.Utils;
using SolarWinds.Coding.Utils.Logger;
using SolarWinds.Coding.Utils.Windows.Logger;

namespace SahredMemoryUser
{

    

    class Program
    {
        static void Main(string[] args)
        {

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

        static async Task A()
        {
            IAsyncLock al = null;
            using (await al.LockAsync())
            {


            }
        }
    }


    public interface IAsyncLock
    {
        Task<IDisposable> LockAsync();
    }

    public interface ILogger
    {
        void Debug(string s);
        void Warn(string s, Exception e);
    }

    public class EmptyLogger: ILogger
    {
        public static readonly ILogger Instance = new EmptyLogger();
        private EmptyLogger()
        {  }
        public void Debug(string s)
        { }

        public void Warn(string s, Exception e)
        { }
    }

}
