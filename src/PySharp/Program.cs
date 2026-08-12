using System.Runtime.CompilerServices;
using PySharp.Runtime;
using PySharp.Runtime.Objects;
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

        var code = new CodeObject()
        {
            StackSize = 4,
            Instructions = [
                new(Opcode.LdVar, 0, 0),    // Load 'print'
                new(Opcode.LdConst, 1, 0),  // Load "bau bau!"
                new(Opcode.LdConst, 2, 3),  // Load 69
                new(Opcode.LdConst, 3, 1),  // Load empty dict
                new(Opcode.CallK, 1, 0, 2), // Call 'print'
                new(Opcode.RetC, 2)         // Return 'None'
            ],
            VarNames = [
                "print",
            ],
            Constants = [
                (PsString)"bau bau!",
                PsDict.Empty,
                PsConstants.None,
                (PsInteger)69,
            ]
        };

        var env = new Runtime.Environment();
        env.Scopes.Push(Builtins.BuiltinsScope);

        engine.RunCode(code, [], env);
    }
}
