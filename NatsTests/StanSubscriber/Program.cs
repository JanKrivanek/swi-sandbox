using System;

namespace StanSubscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            Subscriber.Run();

            Console.WriteLine("Press a key to exit..");
            Console.ReadKey();
        }
    }
}
