using System.Runtime.CompilerServices;
using PySharp.Runtime;
using PySharp.Runtime.Objects;
using PySharp.SyntaxAnalysis;
using PySharp.VM;

[assembly: InternalsVisibleTo("PySharp.Tests")]

namespace PySharp;

public static class Program
{
    public static void Main(string[] args) => runFile(args);

    private static void runFile(string[] args)
    {
        // foreach (string arg in args)
        // {
        //     if (!File.Exists(arg))
        //     {
        //         Console.WriteLine($"File {arg} does not exists.");
        //         System.Environment.Exit(1);
        //     }

        //     string source = File.ReadAllText(arg);

        //     var sync = SynchronizationPoint.ClearPoint(new StringBuffer(source));

        //     var tokenizer = new Tokenizer(sync);
        //     var parser = new PythonParser(new TokenNodeStream(tokenizer));

        //     var tree = parser.Parse();

        //     if (tree != null)
        //     {
        //         var fileView = tree.GetView(0, null);
        //         fileView.SyntaxTree = new SyntaxViewTree
        //         {
        //             Root = fileView,
        //             PositionMap = tokenizer.PositionMap
        //         };

        //         var interpreter = new Interpreter();
        //         interpreter.LoadBuiltins();

        //         interpreter.InterpretFile(fileView);
        //     }
        //     else
        //     {
        //         Console.WriteLine($"Parsing error in file {arg}. Line: {tokenizer.Synchronize().StartLine + 1}");
        //     }
        // }

        var engine = new Engine();

        var builder = new CodeBuilder();

        var left = builder.LdConst((PsString)"bau ");
        var right = builder.LdConst((PsInteger)5);
        var res = builder.RegisterToRegister(Opcode.Mul, left.Dest!, right.Dest!);

        var print = builder.LdVar("print");
        var arg0 = builder.LdConst((PsString)"Bau bau!");
        builder.LdConst((PsInteger)69);
        builder.Move(res.Dest!);
        builder.LdConst(PsDict.Empty);
        builder.CallK(print.Dest!, arg0.Dest!, 3);
        builder.Ret(arg0.Dest!);

        var code = builder.Dump();

        var env = new Runtime.Environment();
        env.Scopes.Push(Builtins.BuiltinsScope);

        engine.RunCode(code, [], env);
    }
}
