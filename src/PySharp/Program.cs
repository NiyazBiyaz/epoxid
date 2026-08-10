using System.Runtime.CompilerServices;
using PySharp.Runtime;
using PySharp.Runtime.Objects;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;
using PySharp.VM;

[assembly: InternalsVisibleTo("PySharp.Tests")]

namespace PySharp;

public static class Program
{
    public static void Main(string[] args) => runFile(args);

    private static void runFile(string[] args)
    {
        foreach (string arg in args)
        {
            if (!File.Exists(arg))
            {
                Console.WriteLine($"File {arg} does not exists.");
                Environment.Exit(1);
            }

            string source = File.ReadAllText(arg);

            var sync = SynchronizationPoint.ClearPoint(new StringBuffer(source));

            var tokenizer = new Tokenizer(sync);
            var parser = new PythonParser(new TokenNodeStream(tokenizer));

            var tree = parser.Parse();

            if (tree != null)
            {
                var fileView = tree.GetView(0, null);
                fileView.SyntaxTree = new SyntaxViewTree
                {
                    Root = fileView,
                    PositionMap = tokenizer.PositionMap
                };

                var interpreter = new Interpreter();
                interpreter.LoadBuiltins();

                interpreter.InterpretFile(fileView);
            }
            else
            {
                Console.WriteLine($"Parsing error in file {arg}. Line: {tokenizer.Synchronize().StartLine + 1}");
            }
        }

        var engine = new Engine();

        var code = new CodeObject()
        {
            StackSize = 2,
            Instructions = [
                new(Opcode.LdConst, 0, 0),
                new(Opcode.LdConst, 1, 1),
                new(Opcode.Add, 0, 0, 1),
                new(Opcode.LdConst, 1, 2),
                new(Opcode.Mul, 0, 1, 0),
                new(Opcode.Ret, 0, 0, 0),
            ],
            Constants = [
                (PsInteger)2,
                (PsInteger)3,
                (PsString)"bau bau "
            ]
        };
        engine.RunCode(code);
    }
}
