using System.Runtime.CompilerServices;
using Epoxid.CodeGenV2;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;
using Epoxid.Runtime;
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

        var fileView = tree.GetView(0, null);
        fileView.SyntaxTree = new SyntaxViewTree
        {
            Root = fileView,
            PositionMap = tokenizer.PositionMap,
        };

        var builder = new CodeBuilder();
        var generator = new BlockGenerator(fileView);

        generator.GenerateCode(builder);

        builder.ResolveRegisterAddresses();
        builder.Optimize();

        var code = builder.Compile();

        Console.WriteLine(code);
        Console.WriteLine("-------------");

        var engine = new Engine();
        var env = new Runtime.Environment();
        env.Scopes.Push(Builtins.BuiltinsScope);

        engine.RunCode(code, [], env);

        return 0;
    }
}
