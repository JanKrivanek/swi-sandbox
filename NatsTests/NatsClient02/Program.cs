using System;

namespace NatsClient02
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            Publisher.Run();
            Console.WriteLine("Press a key to exit..");
            Console.ReadKey();
        }
    }
}
