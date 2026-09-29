
using System.Diagnostics;
using Epoxid.CodeGen;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.Tests.CodeGen;

public class TestFunctionGenerator
{
    [Fact]
    public void TestValidateParameters_AllFine()
    {
        const string src = """
        def bau(fluffy, fuzzy): ...

        """;
        test(src);
    }

    [Fact]
    public void TestValidateParameters_AllFine_Full()
    {
        const string src = """
        def bau(ponde, ring, /, fluffy, fuzzy=moco, *pats, fuwa, moco, **doggos): ...

        """;
        test(src);
    }

    [Fact]
    public void TestValidateParameters_DefaultOrder()
    {
        const string src = """
        def bau(bau=1, baubau): ...

        """;
        Assert.Throws<SyntaxErrorException>(() => test(src));
    }

    [Fact]
    public void TestValidateParameters_RequireParamBeforeSlash()
    {
        const string src = """
        def bau(/, bau): ...

        """;
        Assert.Throws<SyntaxErrorException>(() => test(src));
    }

    [Fact]
    public void TestValidateParameters_RequireParamAfterBareStar()
    {
        string src = """
        def bau(bau, *): ...

        """;
        Assert.Throws<SyntaxErrorException>(() => test(src));

        src = """
        def bau(bau, *pats): ...

        """;
        test(src);

        src = """
        def bau(bau, *, **kwargs): ...

        """;
        Assert.Throws<SyntaxErrorException>(() => test(src));
    }

    // TODO: maybe add test cases for many slashes and stuff like that or param names

    private static void test(string src)
    {
        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));
        var parser = new PythonParser(new TokenNodeStream(tokenizer));
        var module = parser.Parse();

        Debug.Assert(module != null);

        var view = module.GetView(0, null);
        view.SyntaxTree = new SyntaxViewTree
        {
            Root = view,
            PositionMap = tokenizer.PositionMap,
        };

        var func = view.Statements[0] as FunctionDefView ?? throw new UnreachableException("Code doesn't contain function definition");
        var generator = new FunctionGenerator();
        generator.GenerateCodeObject(func, []);
    }
}
