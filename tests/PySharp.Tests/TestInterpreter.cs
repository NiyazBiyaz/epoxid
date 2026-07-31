using PySharp.Runtime;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;

namespace PySharp.Tests;

public class TestInterpreter
{
    [Fact]
    public void TestPrintFunction()
    {
        const string src = """
        print("bau bau!")

        """;
        var file = getView(src);
        var interpreter = getInterpreterStdout(out var stdout);

        interpreter.InterpretFile(file);

        Assert.Equal("bau bau!\n", stdout.ToString());
    }

    private static FileView getView(string src)
    {
        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));
        var parser = new PythonParser(new TokenNodeStream(tokenizer));
        var node = parser.Parse();

        if (node == null)
        {
            Assert.Fail("Given code is invalid");
        }

        var view = node.GetView(0, null);
        view.SyntaxTree = new SyntaxViewTree
        {
            Root = view,
            PositionMap = tokenizer.PositionMap,
        };

        return view;
    }

    private static Interpreter getInterpreter()
    {
        var inter = new Interpreter();
        inter.LoadBuiltins();
        return inter;
    }

    private static Interpreter getInterpreterStdout(out StringWriter stdout)
    {
        var inter = getInterpreter();
        inter.Stdout = stdout = new StringWriter();
        return inter;
    }
}
