using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

public partial class Interpreter
{
    public void LoadBuiltins()
    {
        constructFunc("print", print, print_params);
        constructFunc("input", input, input_params);
        scopes.Push(builtins);
    }

    private void constructFunc(string name, TernaryFunction function, FunctionParametersDescription description)
    {
        var func = new PsBuiltinFunction(name, function)
        {
            ParamsDescription = description,
        };
        builtins.Bind(name, func);
    }

    private static readonly FunctionParametersDescription print_params = new()
    {
        VariadicPositionalParam = FunctionParameter.Args,
        KeywordOnlyParams = [new("end", false), new("file", false), new("sep", false)],
    };

    private PsNone print(PsObject self, PsObject args, PsObject kwargs)
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

        var argsTuple = args as PsTuple ?? throw new ArgumentException("Not a Py# tuple.", nameof(args));

        bool needSep = false;
        foreach (var obj in argsTuple)
        {
            if (needSep)
                Stdout.Write(sep);

            Stdout.Write(obj);

            needSep = true;
        }
        Stdout.Write(end);

        return PsConstants.None;
    }

    private static readonly FunctionParametersDescription input_params = new()
    {
        FreeParams = [new("prompt", false)]
    };

    private PsString input(PsObject self, PsObject args, PsObject kwargs)
    {
        var argsTuple = args as PsTuple ?? throw new ArgumentException("Not a Py# tuple.", nameof(args));

        if (argsTuple.Count == 1)
        {
            Stdout.Write(argsTuple[0]);
        }

        var input = Stdin.ReadLine() ?? throw new Exception("EOFError()");

        return (PsString)input;
    }
}
