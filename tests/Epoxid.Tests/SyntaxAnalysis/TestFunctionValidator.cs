using System.Diagnostics;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.Tests.SyntaxAnalysis;

public class TestFunctionValidator
{
    // TODO: collect errors and warnings to fail with maximum of info (or not fail if IDE mode or something)
    [Fact]
    public void TestValidateParameters_AllFine()
    {
        const string src = """
        def bau(fluffy, fuzzy): ...

        """;
        var validator = getValidator(src);
        var result = validator.ValidateParameters();
        Assert.Equal(ValidationResult.ResultSuccess, result);
    }

    [Fact]
    public void TestValidateParameters_AllFine_Full()
    {
        const string src = """
        def bau(ponde, ring, /, fluffy, fuzzy=moco, *pats, fuwa, moco, **doggos): ...

        """;
        var validator = getValidator(src);
        var result = validator.ValidateParameters();
        Assert.Equal(ValidationResult.ResultSuccess, result);
    }

    [Fact]
    public void TestValidateParameters_DefaultOrder()
    {
        const string src = """
        def bau(bau=1, baubau): ...

        """;
        var validator = getValidator(src);
        var result = validator.ValidateParameters();
        Assert.Equal(ValidationResult.ErrorDefaultOrder, result);
    }

    [Fact]
    public void TestValidateParameters_RequireParamBeforeSlash()
    {
        const string src = """
        def bau(/, bau): ...

        """;
        var validator = getValidator(src);
        var result = validator.ValidateParameters();
        Assert.Equal("at least one parameter must precede positional-only marker", ((ValidationResult.Error)result).Message);
    }

    [Fact]
    public void TestValidateParameters_RequireParamAfterBareStar()
    {
        string src = """
        def bau(bau, *): ...

        """;
        var validator = getValidator(src);
        var result = validator.ValidateParameters();
        Assert.Equal("at least one parameter must follow bare '*'", ((ValidationResult.Error)result).Message);

        src = """
        def bau(bau, *pats): ...

        """;
        validator = getValidator(src);
        result = validator.ValidateParameters();
        Assert.Equal(ValidationResult.ResultSuccess, result);

        src = """
        def bau(bau, *, **kwargs): ...

        """;
        validator = getValidator(src);
        result = validator.ValidateParameters();
        Assert.Equal("at least one parameter must follow bare '*'", ((ValidationResult.Error)result).Message);
    }

    // TODO: maybe add test cases for many slashes and stuff like that or param names

    private static FunctionValidator getValidator(string src)
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

        var func = (view.Statements[0] as FunctionDefView)!.FunctionDef;
        return new FunctionValidator(func);
    }
}
