using System;

namespace StanPublisher
{
    class Program
    {
        static void Main(string[] args)
        {
            Publisher.Run();
            Console.WriteLine("Press a key to exit..");
            Console.ReadKey();
        }
    }
}
