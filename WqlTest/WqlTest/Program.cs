using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WqlTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Test1.Run();

            Console.WriteLine("Will exit after key press...");
            Console.ReadKey();
        }
    }
}
