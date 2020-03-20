using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

// Reference the NATS client.
using NATS.Client;
using STAN.Client;

namespace StanPublisher
{
    public static class Publisher
    {
        public static async void Run()
        {
            // Create a new connection factory to create
            // a connection.
            StanConnectionFactory cf = new StanConnectionFactory();

            IStanConnection c = cf.CreateConnection("test-cluster", "test-client-pub");

            var g = await c.PublishAsync("foo", Encoding.UTF8.GetBytes("hello world"));

            //c.Publish("foo2", Encoding.UTF8.GetBytes("hello world 2"));




            //PerfTest(c, "foo");
            try
            {
                SendOnePerKeypress(c, "foo");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            



            // Closing a connection
            c.Close();
        }

        private static void PerfTest(IStanConnection c, string subject)
        {
            const int msgsCount = 100;
            const int rounds = 100;
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
                    c.PublishAsync(subject, messages[i]);
                }
            }
            sw.Stop();

            Console.WriteLine("{0} messages send in: {1}", rounds * msgsCount, sw.Elapsed);
        }

        private static void SendOnePerKeypress(IStanConnection c, string subject)
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
