using System;

namespace DependencyAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (DependencyAnalyzer analyzer = new DependencyAnalyzer("Result.csv"))
            {
                analyzer.Analyze(args[0]);
            }
        }
    }
}
