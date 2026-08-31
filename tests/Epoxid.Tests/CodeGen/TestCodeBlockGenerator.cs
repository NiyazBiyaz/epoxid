using System.Diagnostics;
using Epoxid.CodeGen;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.Tests.CodeGen;

public class TestCodeBlockGenerator
{
    [Fact]
    public void TestCanGenerate_Condition()
    {
        const string src = """
        if a and b or c:
            ...

        """;
        tryGenerate(src);
    }

    private static void tryGenerate(string src)
    {
        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));
        var parser = new PythonParser(new TokenNodeStream(tokenizer));
        var fileNode = parser.Parse();

        Debug.Assert(fileNode != null);

        var fileView = fileNode.GetView(0, null);
        fileView.SyntaxTree = new SyntaxViewTree
        {
            Root = fileView,
            PositionMap = tokenizer.PositionMap,
        };

        var generator = new CodeBlockGenerator(fileView);
        var result = generator.GenerateCode();

        Assert.Equal(ValidationResult.ResultSuccess, result);
    }
}
