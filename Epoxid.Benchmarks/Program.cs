using BenchmarkDotNet.Running;

namespace Epoxid.Benchmarks;

public static class Program
{
    public static void Main()
    {
        var summary = BenchmarkRunner.Run([typeof(BenchTokenizer), typeof(BenchParser)]);
    }
}
