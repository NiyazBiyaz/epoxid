using System.Runtime.CompilerServices;
using Epoxid.VM;
using Epoxid.SyntaxAnalysis;
using Epoxid.Runtime;
using Epoxid.CodeGen;
using Epoxid.Runtime.Objects;

[assembly: InternalsVisibleTo("Epoxid.Tests")]

namespace Epoxid;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
            return 1;

        var fileContent = File.ReadAllText(args[0]);

        var fileView = PythonParser.ParseModuleView(fileContent.AsMemory());

        RuntimeInitializer.InitializeObjectType();

        var env = new EpEnvironment()
        {
            Builtins = Builtins.BuiltinsScope,
            Module = new(),
        };

        var code = CodeFactory.CompileModule(fileView, "__main__");

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
