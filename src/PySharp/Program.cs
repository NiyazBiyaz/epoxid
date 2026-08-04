using System.Runtime.CompilerServices;
using PySharp.Runtime;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;

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
    }
}
