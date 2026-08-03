using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;

namespace PySharp.Tests.SyntaxAnalysis;

public class TestPythonParser
{
    [Fact]
    public Task TestParse_BauBau()
    {
        const string src = """
        print("Bau bau!")

        """;
        var parser = getParser(src);

        var res = parser.Parse();

        Assert.NotNull(res);

        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_TwoPlusTwoMultiplyTwo()
    {
        const string src = """
        2 + 2 * 2

        """;
        var parser = getParser(src);

        var res = parser.Parse();

        Assert.NotNull(res);

        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_TrueFalseNone()
    {
        const string src = """
        True
        False
        None

        """;
        var parser = getParser(src);

        var res = parser.Parse();

        Assert.NotNull(res);

        return Verify(res.PrettyPrint());
    }

    private static PythonParser getParser(string src)
    {
        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));
        var parser = new PythonParser(new TokenNodeStream(tokenizer));
        return parser;
    }
}
