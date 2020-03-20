using System;

namespace NatsClient01
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            Subscriber.Run();

            Console.WriteLine("Press a key to exit..");
            Console.ReadKey();
        }
    }
}
