using System.Runtime.CompilerServices;
using Epoxid.VM;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;
using Epoxid.Runtime;
using Epoxid.CodeGen;

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

        var env = new EpEnvironment()
        {
            Builtins = Builtins.BuiltinsScope,
            Module = new(),
        };

        var st = SymbolTable.Create(fileView, "__main__");
        var builder = new CodeBuilder(st);

        BlockGenerator.Generate(builder, fileView);

        var code = builder.Compile();

#if DEBUG
        foreach (var instr in code.Instructions)
        {
            Console.WriteLine(instr);
        }

        Console.WriteLine("==============================");
#endif

        var engine = Engine.CurrentEngine;
        engine.RunCode(code, [], env);

        return 0;
    }
}
