using System.Runtime.CompilerServices;
using Epoxid.Runtime;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;
using Epoxid.VM;

[assembly: InternalsVisibleTo("Epoxid.Tests")]

namespace Epoxid;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
            return 1;

        var file = File.ReadAllText(args[0]);

        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(file)));
        var parser = new PythonParser(new TokenNodeStream(tokenizer));

        var tree = parser.Parse();

        if (tree == null)
        {
            Console.Error.WriteLine("Error while parsing file");
            return 1;
        }

        var view = tree.GetView(0, null);
        view.SyntaxTree = new SyntaxViewTree
        {
            Root = view,
            PositionMap = tokenizer.PositionMap,
        };

        var codeGen = new CodeBlockGenerator(view.Statements);
        codeGen.GenerateCode();

        var code = codeGen.Builder.Dump();

        var engine = new Engine();

        var environment = new Runtime.Environment();
        environment.Scopes.Push(Builtins.BuiltinsScope);

        // Console.WriteLine(code.ToString());
        // Console.WriteLine("----------------");

        engine.RunCode(code, [], environment);

        return 0;
    }
}
