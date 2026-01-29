using System;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            //var a = new StringMathematicsBenchmark();
            //a.StringSum();

            var b = new AverageCalculationBenchmark();
            b.Setup();
            Console.WriteLine(b.AverageOnStringAsIntegers());
            Console.WriteLine(b.AverageOnIntegersWithCustomIndexOf());
            Console.WriteLine(b.AverageOnIntegers());
            Console.WriteLine(b.AverageOnDoubles());
#else

            //var _ = BenchmarkRunner.Run(typeof(Program).Assembly);
            BenchmarkRunner.Run<AverageCalculationBenchmark>();
#endif
        }
    }
}
