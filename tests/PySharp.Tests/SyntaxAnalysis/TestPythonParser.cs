using DiffEngine;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;

namespace PySharp.Tests.SyntaxAnalysis;

public class TestPythonParser
{
    static TestPythonParser()
    {
        DiffRunner.Disabled = true;
    }

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

    [Fact]
    public Task TestParse_IfElseStatement()
    {
        const string src = """
        if fluffy:
            bau()
        else: baubau()

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_IfElifStatement()
    {
        const string src = """
        if fluffy:
            bau()
        elif fuzzy:
            baubau()

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_IfElifElseStatement()
    {
        const string src = """
        if fluffy:
            bau()
        elif fuzzy:
            baubau()
        elif cute:
            baubaubau()
        else:
            paco()

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_While()
    {
        const string src = """
        while ponDeRing:
            bau()
        else:
            baubau()

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_For()
    {
        const string src = """
        for i in range(10):
            bau(i)
        else:
            baubau()

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
