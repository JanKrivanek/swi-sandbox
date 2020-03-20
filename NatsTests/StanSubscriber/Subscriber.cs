using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

// Reference the NATS client.
//using NATS.Client;
using STAN.Client;

namespace StanSubscriber
{
    public static class Subscriber
    {
        public static void Run()
        {
            // Create a new connection factory to create
            // a connection.
            StanConnectionFactory cf = new StanConnectionFactory();

            IStanConnection c = cf.CreateConnection("test-cluster", "test-client-sub");

            //StanOptions.GetDefaultOptions().

            Stopwatch sw = new Stopwatch();
            int msgs = 0;
            const int messagesCount = 10000;

            //// Setup an event handler to process incoming messages.
            //// An anonymous delegate function is used for brevity.
            //EventHandler<StanMsgHandlerArgs> h = (sender, args) =>
            //{
            //    msgs++;
            //    if (msgs == messagesCount)
            //    {
            //        sw.Stop();
            //        Console.WriteLine("{0} messages received in: {1}", messagesCount, sw.Elapsed);
            //    }
            //    else if (msgs == 1)
            //    {
            //        sw.Start();
            //    }
            
            //};



            EventHandler<StanMsgHandlerArgs> h = (sender, args) =>
            {
                // print the message
                Console.WriteLine(Encoding.UTF8.GetString(args.Message.Data));

                // Here are some of the accessible properties from
                // the message:
                // args.Message.Data;
                // args.Message.Reply;
                // args.Message.Subject;
                // args.Message.ArrivalSubcription.Subject;
                // args.Message.ArrivalSubcription.QueuedMessageCount;
                // args.Message.ArrivalSubcription.Queue;

                // Unsubscribing from within the delegate function is supported.
                //args.Message.ArrivalSubcription.Unsubscribe();
            };

            // The simple way to create an asynchronous subscriber
            // is to simply pass the event in.  Messages will start
            // arriving immediately.
            //IAsyncSubscription s = c.SubscribeAsync("foo", h);

            // Alternatively, create an asynchronous subscriber on subject foo,
            // assign a message handler, then start the subscriber.   When
            // multicasting delegates, this allows all message handlers
            // to be setup before messages start arriving.


            var opts = StanSubscriptionOptions.GetDefaultOptions();
            opts.DurableName = "my-durable";
            //opts.DeliverAllAvailable();


            IStanSubscription sAsync = c.Subscribe("foo", opts, h);

            //// Simple synchronous subscriber
            //ISyncSubscription sSync = c.SubscribeSync("foo");

            //// Using a synchronous subscriber, gets the first message available,
            //// waiting up to 1000 milliseconds (1 second)
            //Msg m = sSync.NextMessage(1000);

            //c.Publish("foo", Encoding.UTF8.GetBytes("hello world"));

            Console.WriteLine("Will quit after key press...");
            Console.ReadKey();

            // Closing a connection
            c.Close();
        }
    }
}
