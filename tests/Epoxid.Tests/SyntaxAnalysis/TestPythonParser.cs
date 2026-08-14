using DiffEngine;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.Tests.SyntaxAnalysis;

public class TestPythonParser
{
    static TestPythonParser()
    {
        // It works just awful for me. In some stupid reason it want to use neovim, and even in this situation can't
        // just normally launch it to show me two files. lol.
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

    [Fact]
    public Task TestParse_Assignment()
    {
        const string src = """
        b = ...
        a = True
        u = 789

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_Imports()
    {
        const string src = """
        import bau
        import bau.bau as Bau
        from fwmc import fuzzy as moco, fluffy as fuwa
        from fwmc import (
            fuwa,
            moco,
        )
        from .pondering import baubau
        from ....halo.halo import baubau as bauBau

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestParse_LazyImports()
    {
        const string src = """
        lazy import bau
        lazy import bau.bau as Bau
        lazy from fwmc import fuzzy as moco, fluffy as fuwa
        lazy from fwmc import (
            fuwa,
            moco,
        )
        lazy from .pondering import baubau
        lazy from ....halo.halo import baubau as bauBau

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestFunctionParams_Ordinal()
    {
        const string src = """
        def bau(fluffy, fuzzy): ...

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestFunctionParams_Full()
    {
        const string src = """
        def bau(pon, de, ring, /, fluffy, fuzzy, *pats, fuwa, moco, **kwargs): ...

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestFunctionParams_Full_WithDefaults()
    {
        const string src = """
        def bau(pon, de, ring, /, fluffy=fuwawa, fuzzy=mococo, *pats, fuwa=iyargh, moco=hoeh, **doggos): ...

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestFunctionParams_BeforeSlash()
    {
        const string src = """
        def bau(pon, de, ring, /): ...

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestFunctionParams_BeforeStar()
    {
        const string src = """
        def bau(fluffy, fuzzy, *pats): ...

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public Task TestFunctionParams_AfterStar()
    {
        const string src = """
        def bau(*, fuwa, moco): ...

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.NotNull(res);
        return Verify(res.PrettyPrint());
    }

    [Fact]
    public void TestFunctionParams_CannotSetDefaultTo_ArgsKwargs()
    {
        string src = """
        def bau(*pats=many): ...

        """;
        var parser = getParser(src);
        var res = parser.Parse();
        Assert.Null(res); // TODO: when added invalid nodes, replace with `Invalid` flag check

        src = """
        def bau(*doggos=happy): ...

        """;
        parser = getParser(src);
        res = parser.Parse();
        Assert.Null(res); // TODO: see above
    }

    [Fact]
    public Task TestFunctionParams_Annotations()
    {
        const string src = """
        def bau(fuwa: Fluffy, moco: Fuzzy): ...

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
