using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;

namespace NatsClient02
{
    public static class InterlockedEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Read(ref int value)
        {
            return Interlocked.CompareExchange(ref value, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckAndFlipState(ref int value, int newState, int expectedState)
        {
            return Interlocked.CompareExchange(ref value, newState, expectedState) == expectedState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckAndFlipState(ref long value, long newState, long expectedState)
        {
            return Interlocked.CompareExchange(ref value, newState, expectedState) == expectedState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckAndFlipState<T>(ref T value, T newState, T expectedState) where T : class
        {
            return Interlocked.CompareExchange(ref value, newState, expectedState) == expectedState;
        }
    }

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message);
        void Log(LogLevel level, string messageFmt, params object[] args);
        void LogException(LogLevel level, string message, Exception e);
        void LogException(LogLevel level, Exception e, string messageFmt, params object[] args);
    }

    public class Logger : ILogger
    {
        public void Log(LogLevel level, string message)
        {
            Console.WriteLine(message);
        }

        public void Log(LogLevel level, string messageFmt, params object[] args)
        {
            Console.WriteLine(messageFmt, args);
        }

        public void LogException(LogLevel level, string message, Exception e)
        {
            Console.WriteLine(message);
            Console.WriteLine(e);
        }

        public void LogException(LogLevel level, Exception e, string messageFmt, params object[] args)
        {
            Console.WriteLine(messageFmt, args);
            Console.WriteLine(e);
        }
    }

    public class SafePublisher
    {

        public static void PerfTest()
        {
            SafePublisher pub = new SafePublisher(new Logger(), "Blah");
            pub.ConnectingTask.Wait();
            Console.WriteLine("Connected");

            const int msgsCount = 1000;
            const int rounds = 1000000;
            byte[][] messages = new byte[msgsCount][];

            for (int i = 0; i < msgsCount; i++)
            {
                messages[i] = Encoding.UTF8.GetBytes(i.ToString() + " hello world");
            }

            string channel = "foo";

            int successMsgs = 0;
            int failMsgs = 0;

            Stopwatch sw = new Stopwatch();

            sw.Start();
            for (int round = 0; round < rounds; round++)
            {
                for (int i = 0; i < msgsCount; i++)
                {
                    if(pub.SendRequest(channel, messages[i]))
                    {
                        successMsgs++;
                    }
                    else
                    {
                        failMsgs++;
                        if (!pub.ConnectingTask.Wait(TimeSpan.FromSeconds(1)))
                        {
                            Console.WriteLine("Publisher not yet reconnected waiting...");
                        }
                    }
                }
            }
            sw.Stop();

            Console.WriteLine("{0} messages send in: {1}", rounds * msgsCount, sw.Elapsed);
        }

        //we can afford have this not synchronized - and attempt to send some more msgs while not connected (exc)
        // and vice versa.
        // faster in main scenario
        private bool _connected;
        private IConnection _connection;
        private readonly ILogger _logger;
        private readonly string _clientIdentity;
        private static readonly TimeSpan[] _retryIntervals = new TimeSpan[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
        };

        //TODO: connect options
        public SafePublisher(ILogger logger, string clientIdentity)
        {
            _logger = logger;
            _clientIdentity = clientIdentity;
            ConnectingTask = DoConnect();
        }

        public Task ConnectingTask { get; private set; }

        private int _isConnecting = 0;
        private async Task DoConnect()
        {
            if (!InterlockedEx.CheckAndFlipState(ref _isConnecting, 1, 0))
            {
                return;
            }

            _connected = false;
            try
            {
                _connection?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogException(LogLevel.Info, "Exception during disposing broken channel.", e);
            }
            
            _connection = null;

            //since the nats client have no async connect
            await Task.Yield();

            ConnectionFactory cf = new ConnectionFactory();
            int attempts = 0;
            do
            {
                try
                {
                    _connection = cf.CreateConnection();
                    _connected = true;
                    _logger.Log(LogLevel.Info, "Channel connected.");

                    ////TODO: only if not already subscribed (but what on reconnect - need to try)
                    //IAsyncSubscription sAsync = _connection.SubscribeAsync(_clientIdentity);
                    //sAsync.MessageHandler += HandleMsg;
                    ////Todo: GC root this
                    //sAsync.Start();
                }
                //NATS.Client.NATSNoServersException
                catch (Exception e)
                {
                    attempts++;
                    int retryTimeoutIdx = Math.Min(attempts, _retryIntervals.Length) - 1;
                    TimeSpan delay = _retryIntervals[retryTimeoutIdx];
                    _logger.LogException(LogLevel.Error, e,
                        "Failed to connect to message bus. Attempt #{0}. Will retry after: {1}", attempts, delay);
                    await Task.Delay(delay);
                }
            } while (!_connected);

            //CLI has write through - no reorder of reads after writes
            _isConnecting = 0;
        }

        private void Foo()
        {
            ConnectionFactory cf = new ConnectionFactory();
            //
            IConnection c = cf.CreateConnection();
            //System.IO.IOException
            c.Publish("foo", Encoding.UTF8.GetBytes("hello world"));
        }

        public bool SendRequest(string channel, byte[] msg)
        {
            //TODO: return error result instead
            if (!_connected)
            {
                //throw new Exception("Currently not connected");
                return false;
            }


            try
            {
                //TODO: pre-store targets to const array. This way we can have preffixes
                // e.g.: Sensor:Snmp
                //it's thread safe (big fat lock in NATS)
                _connection.Publish(channel, msg);
                return true;
            }
            //catch (NATSException e)
            //catch (System.IO.IOException e)
            catch (Exception e)
            {
                _logger.LogException(LogLevel.Error, $"Error during sending the client message [{msg[0]}]", e);
                ConnectingTask = DoConnect();

                //TODO: return error result instead
                //throw new Exception("Currently not connected");
                return false;
            }
        }


        //public void SendRequest(ISensorRequestInternal sensorRequest)
        //{
        //    //TODO: return error result instead
        //    if (_connected)
        //    {
        //        throw new Exception("Currently not connected");
        //    }

        //    //todo: pool and reuse (but actually might not be needed at all)
        //    byte[] serializedBytes;
        //    using (var stream = new MemoryStream())
        //    {
        //        Serializer.Serialize(stream, sensorRequest);
        //        serializedBytes = stream.ToArray();
        //    }

        //    try
        //    {
        //        //TODO: pre-store targets to const array. This way we can have preffixes
        //        // e.g.: Sensor:Snmp
        //        //it's thread safe (big fat lock in NATS)
        //        _connection.Publish(sensorRequest.LogicRunnerIdentifier.ToString(), serializedBytes);
        //    }
        //    //catch (NATSException e)
        //    //catch (System.IO.IOException e)
        //    catch (Exception e)
        //    {
        //        _logger.LogException(LogLevel.Error, $"Error during sending the client message [{sensorRequest.SensorRequestUniqueIdentifier}]", e);
        //        DoConnect();

        //        //TODO: return error result instead
        //        throw new Exception("Currently not connected");
        //    }
        //}

        private void HandleMsg(object sender, MsgHandlerEventArgs msg)
        {
            //todo
        }

        //public event Action<SensorResponseInternal> ResponseAvailable;
    }
}
