using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCommunication.RateLimiter;

namespace SahredMemoryUser
{

    

    class Program
    {
        static void Main(string[] args)
        {

            TestBench tb = new TestBench();
            tb.RunTest();

            //new MMFTests().Run();

            //new TestRate().RunTest();

            //new Test().Run();
            //new Test().Run();
        }
    }

    public class Test
    {
        private const string sharedMutexName = @"MyMutex";

        private ILogger CreateLogger(Type T)
        {
            return EmptyLogger.Instance;
        }

        public void Run()
        {
            //Mutex mutex;

            //bool created;
            //mutex = new Mutex(false, sharedMutexName, out created, new MutexSecurity());

            //if (!Mutex.TryOpenExisting(sharedMutexName, MutexRights.Synchronize, out mutex))
            //{
                
            //}

            MutexFactory mf = new MutexFactory(CreateLogger);
            IMutex m = mf.Create(sharedMutexName);

            while (true)
            {
                for (int i = 0; i < 100;)
                {
                    using (m.Lock())
                    {
                        Thread.Sleep(new Random().Next(200));
                        i++;
                    }
                }

                Console.WriteLine("Mutex acquired 100 times");
                Thread.Sleep(new Random().Next(2000));
            }
        }
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

    public interface IMutexFactory
    {
        IMutex Create(string name);
    }

    internal sealed class MutexFactory : IMutexFactory
    {
        private readonly ILogger logger;
        private readonly Func<Type, ILogger> loggerFactory;

        public MutexFactory(Func<Type, ILogger> loggerFactory)
        {
            //Require.NotNull(loggerFactory, nameof(loggerFactory));

            this.loggerFactory = loggerFactory;
            logger = loggerFactory(GetType());
        }

        public IMutex Create(string name)
        {
            logger.Debug($"Starting created the mutex with name {name}.");

            var id = $@"Global\{name}";
            var mutex = InitializeMutex(id);
            var result = new MutexWrapper(loggerFactory, mutex, id);

            logger.Debug($"Successfully created the mutex with name {name}.");
            return result;
        }

        private Mutex InitializeMutex(string id)
        {
            bool createdNew;
            // WorldSid means EVERYONE group
            var allowEveryoneRule =
                new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            var mutex = new Mutex(false, id, out createdNew, securitySettings);
            if (createdNew)
            {
                logger.Debug($"Created new mutex with ID {id}.");
            }
            else
            {
                logger.Debug($"Opened existing mutex: {id}.");
            }
            return mutex;
        }
    }

    public interface IMutex : IDisposable
    {
        string Id { get; }
        IDisposable Lock();
    }

    

    internal class MutexLockContext : IDisposable
    {
        private readonly Mutex mutex;
        private readonly ILogger logger;
        private readonly string id;

        public MutexLockContext(Mutex mutex,
            Func<Type, ILogger> loggerFactory,
            string id)
        {
            //Require.NotNull(loggerFactory, nameof(loggerFactory));
            //Require.NotEmpty(id, nameof(id));

            this.mutex = mutex;
            logger = loggerFactory(GetType());
            this.id = id;
        }

        public void Dispose()
        {
            logger.Debug($"Releasing mutex {id}.");
            mutex?.ReleaseMutex();
            logger.Debug($"Successfully released the mutex {id}.");
        }
    }

    internal sealed class MutexWrapper : IMutex
    {
        private readonly Mutex mutex;
        private readonly ILogger logger;
        private readonly Func<Type, ILogger> loggerFactory;

        public MutexWrapper(Func<Type, ILogger> loggerFactory,
            Mutex mutex,
            string id)
        {
            //Require.NotNull(loggerFactory, nameof(loggerFactory));
            //Require.NotEmpty(id, nameof(id));
            //Require.NotNull(mutex, nameof(mutex));

            this.loggerFactory = loggerFactory;
            logger = loggerFactory(GetType());
            this.mutex = mutex;
            Id = id;
        }

        public string Id { get; }

        public void Dispose()
        {
            mutex.Dispose();
        }

        public IDisposable Lock()
        {
            logger.Debug($"Started locking the mutex {Id}.");

            try
            {
                if (mutex.WaitOne())
                {
                    logger.Debug($"Successfully locked the mutex {Id}.");
                }
            }
            catch (AbandonedMutexException exc)
            {
                logger.Warn($"'AbandonedMutexException' was thrown. Probably we are taking over mutex '{Id}' after crashed process or thread.", exc);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException(string.Format("Could not wait on mutex [{0}]", Id), exc);
            }
            return new MutexLockContext(mutex, loggerFactory, Id);
        }
    }
}
