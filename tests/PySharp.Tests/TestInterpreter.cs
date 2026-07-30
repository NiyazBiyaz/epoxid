using PySharp.Runtime;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;

namespace PySharp.Tests;

public class TestInterpreter
{
    [Fact]
    public void TestBauBau_AllOkay()
    {
        const string src = """
        print("bau bau!")

        """;
        var file = getView(src);
        var interpreter = getInterpreter();

        interpreter.InterpretFile(file);
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
}
