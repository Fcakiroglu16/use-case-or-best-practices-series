using BenchmarkDotNet.Running;

namespace BenchmarkSuite6
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Use assembly-wide discovery for benchmarks
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
