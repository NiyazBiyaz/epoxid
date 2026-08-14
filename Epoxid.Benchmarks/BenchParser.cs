using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.Benchmarks;

[MemoryDiagnoser]
public class BenchParser
{
    private static readonly BufferedTokenizer pkgutil;
    private static readonly BufferedTokenizer tokens_generator;

    static BenchParser()
    {
        // Tokenize all files before it will be used in parser because actual tokenizer is lazy
        string path = "Data/pkgutil";
        pkgutil = new(File.ReadAllText(path));

        path = "Data/tokens_generate_from_file";
        tokens_generator = new(File.ReadAllText(path));
    }

    [Benchmark]
    public void TestParseBeeg()
    {
        pkgutil.Reset();
        var stream = new TokenNodeStream(pkgutil);
        var parser = new PythonParser(stream);

        parser.Parse();
    }

    [Benchmark]
    public void TestParseSmol()
    {
        tokens_generator.Reset();
        var stream = new TokenNodeStream(tokens_generator);
        var parser = new PythonParser(stream);

        parser.Parse();
    }
}

public class BufferedTokenizer : ITokenizer
{
    public bool ShouldStop => throw new NotImplementedException();
    public TokenizerError Error => throw new NotImplementedException();
    public string? ErrorMessage => throw new NotImplementedException();

    private int index = 0;
    private readonly List<Token> buffer = [];

    public BufferedTokenizer(string src)
    {
        var actuallyTokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));

        while (!actuallyTokenizer.ShouldStop)
        {
            actuallyTokenizer.ReadNext(out var token);
            buffer.Add(token.Value);
        }
    }

    public void Reset() => index = 0;

    public void ReadNext([NotNull] out Token? token) => token = buffer[index++];

    public SynchronizationPoint Synchronize() => throw new NotImplementedException();
}
