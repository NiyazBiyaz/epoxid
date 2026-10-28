using Epoxid.Runtime.Objects;
using Epoxid.SourceGeneration;

namespace Epoxid.Runtime;

public static partial class Builtins
{
    public static readonly Scope BuiltinsScope = new();

    static Builtins()
    {
        constructFunc("print", print, print_parameters);
        constructFunc("input", input, input_parameters);
        BuiltinsScope.Bind("int", EpInteger.Int);
        BuiltinsScope.Bind("float", EpFloat.Float);
        BuiltinsScope.Bind("type", EpType.Type);
    }

    private static void constructFunc(string name, FrameCallFunction function, FunctionParametersDescription description)
    {
        var func = new EpBuiltinFunction(name, function)
        {
            ParamsDescription = description,
        };
        BuiltinsScope.Bind(name, func);
    }

    // Here's should be 'sys.write/read', but let's just let it work for now :P
    [BuiltinFunction("print", @"end=(EpString)""\n""", @"sep=(EpString)"" """)]
    private static EpNone printImpl(ReadOnlySpan<EpObject> args, EpString end, EpString sep)
    {
        bool needSep = false;
        foreach (var arg in args)
        {
            if (needSep)
                Console.Write(sep);
            needSep = true;
            Console.Write(arg);
        }

        Console.Write(end);

        return EpNone.None;
    }

    [BuiltinFunction("input", @"prompt=(EpString)""""")]
    private static EpString inputImpl(EpString prompt)
    {
        Console.Write(prompt);

        var input = Console.ReadLine() ?? throw new Exception("EOFError()");

        return (EpString)input;
    }
}
