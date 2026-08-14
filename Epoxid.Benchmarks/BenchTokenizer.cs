using BenchmarkDotNet.Attributes;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.Benchmarks;

[MemoryDiagnoser]
public class BenchTokenizer
{
    private static readonly string pkgutil;
    private static readonly string tokens_generate;

    static BenchTokenizer()
    {
        string path = "Data/pkgutil";
        pkgutil = File.ReadAllText(path);

        path = "Data/tokens_generate_from_file";
        tokens_generate = File.ReadAllText(path);
    }

    [Benchmark]
    public void TestTokenizeBeeg()
    {
        var buffer = new StringBuffer(pkgutil);
        var sync = SynchronizationPoint.ClearPoint(buffer);

        var tokenizer = new Tokenizer(sync);

        while (!tokenizer.ShouldStop)
            tokenizer.ReadNext(out _);
    }

    [Benchmark]
    public void TestTokenizeSmol()
    {
        var buffer = new StringBuffer(tokens_generate);
        var sync = SynchronizationPoint.ClearPoint(buffer);

        var tokenizer = new Tokenizer(sync);

        while (!tokenizer.ShouldStop)
            tokenizer.ReadNext(out _);
    }
}
