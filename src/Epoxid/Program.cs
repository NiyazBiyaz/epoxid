using System.Runtime.CompilerServices;
using Epoxid.CodeGenV2;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;
using Epoxid.Runtime;
using Epoxid.VM;
using Epoxid.Runtime.Objects;

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

        generator.GenerateModule(builder);

        var code = builder.Compile();

#if DEBUG
        Console.WriteLine(code);

        foreach (var function in builder.Constants.OfType<EpFunction>())
        {
            Console.WriteLine($"Code of function '{function.QualName}':");
            Console.WriteLine(function.Code);
        }

        Console.WriteLine("-------------");
#endif

        var engine = new Engine();
        Engine.SetCurrentEngine(engine);
        var env = new EpEnvironment()
        {
            Builtins = Builtins.BuiltinsScope,
            Module = new(),
        };

        engine.RunCode(code, [], env);

        return 0;
    }
}
