using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

// Сейчас я буду использовать обычные Console.Read/Write методы и глобальное состояние,
// но мне это не нравится.
// Причина в том, что сейчас надо убрать "магический" Interpreter, который по сути должен
// был использовать инверсию зависимости, но конечная реализация `print` и `input` вообще
// должна быть в ПайШарпе при помощи примитивов из sys, которого сейчас и близко нет.
// Так что так...

public static class Builtins
{
    public static readonly Scope BuiltinsScope = new();

    static Builtins()
    {
        constructFunc("print", print, print_params);
        constructFunc("input", input, input_params);
    }

    private static void constructFunc(string name, FrameCallFunction function, FunctionParametersDescription description)
    {
        var func = new PsBuiltinFunction(name, function)
        {
            ParamsDescription = description,
        };
        BuiltinsScope.Bind(name, func);
    }

    private static void constructFunc(string name, FrameKeywordCallFunction function, FunctionParametersDescription description)
    {
        var func = new PsBuiltinFunction(name, function)
        {
            ParamsDescription = description,
        };
        BuiltinsScope.Bind(name, func);
    }

    private static readonly FunctionParametersDescription print_params = new()
    {
        VariadicPositionalParam = FunctionParameter.Args,
        KeywordOnlyParams = [new("end", false), new("file", false), new("sep", false)],
    };

    private static PsNone print(ReadOnlySpan<PsObject> args, PsDict kwargs)
    {
        // Simple 'print' implementation on C# side.
        PsObject? end = null, sep = null;

        if (kwargs is PsDict kwargsDict)
        {
            kwargsDict.TryGetValue((PsString)"end", out end);

            // NSY
            // kwargs.TryGetValue((PsString)"file", out var file)

            kwargsDict.TryGetValue((PsString)"sep", out sep);
        }

        end ??= (PsString)"\n";
        sep ??= (PsString)" ";

        bool needSep = false;
        foreach (var obj in args)
        {
            if (needSep)
                Console.Write(sep);

            Console.Write(obj);

            needSep = true;
        }
        Console.Write(end);

        return PsConstants.None;
    }

    private static readonly FunctionParametersDescription input_params = new()
    {
        FreeParams = [new("prompt", false)]
    };

    private static PsString input(ReadOnlySpan<PsObject> args)
    {
        if (args.Length == 1)
        {
            Console.Write(args[0]);
        }

        var input = Console.ReadLine() ?? throw new Exception("EOFError()");

        return (PsString)input;
    }
}
