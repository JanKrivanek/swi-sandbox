using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

// Reference the NATS client.
using NATS.Client;

namespace NatsClient02
{
    public static class Publisher
    {
        public static void Run()
        {
            // Create a new connection factory to create
            // a connection.
            ConnectionFactory cf = new ConnectionFactory();

            // Creates a live connection to the default
            // NATS Server running locally
            IConnection c = cf.CreateConnection();

            c.Publish("foo", Encoding.UTF8.GetBytes("hello world"));

            //c.Publish("foo2", Encoding.UTF8.GetBytes("hello world 2"));


            // Publish requests to the given reply subject:
            c.Publish("foo", "bar", Encoding.UTF8.GetBytes("help!"));


            PerfTest(c, "foo");
            //SendOnePerKeypress(c, "foo");

            // Draining and closing a connection
            c.Drain();

            // Closing a connection
            c.Close();
        }

        private static void PerfTest(IConnection c, string subject)
        {
            const int msgsCount = 1000;
            const int rounds = 1000;
            byte[][] messages = new byte[msgsCount][];

            for (int i = 0; i < msgsCount; i++)
            {
                messages[i] = Encoding.UTF8.GetBytes("hello world " + i.ToString());
            }

            Stopwatch sw = new Stopwatch();

            sw.Start();
            for (int round = 0; round < rounds; round++)
            {
                for (int i = 0; i < msgsCount; i++)
                {
                    c.Publish(subject, messages[i]);
                }
            }
            sw.Stop();

            Console.WriteLine("{0} messages send in: {1}", rounds * msgsCount, sw.Elapsed);
        }

        private static void SendOnePerKeypress(IConnection c, string subject)
        {
            int msgsCount = 0;
            
            while(true)
            {
                c.Publish(subject, Encoding.UTF8.GetBytes("Message " + (++msgsCount).ToString()));
                Console.ReadKey();
            }
        }
    }
}
