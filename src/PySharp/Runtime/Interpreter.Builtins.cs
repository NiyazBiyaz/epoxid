using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

public partial class Interpreter
{
    public void LoadBuiltins()
    {
        var builtins = new Scope();

        builtins.Bind(nameof(print), new PsBuiltinFunction(nameof(print), print));
        builtins.Bind(nameof(input), new PsBuiltinFunction(nameof(input), input));

        scopes.Push(builtins);
    }

    private PsNone print(PsObject self, PsTuple args, PsDict? kwargs)
    {
        // Simple 'print' implementation on C# side.
        int seenCount = 0;

        PsObject? end = null, sep = null;

        if (kwargs != null)
        {
            if (kwargs.TryGetValue((PsString)"end", out end))
                seenCount++;

            // NSY
            // if (kwargs.TryGetValue((PsString)"file", out var file))
            //     file = Stdout;
            //
            // else
            //     seenCount++;

            if (kwargs.TryGetValue((PsString)"sep", out sep))
                seenCount++;

            if (kwargs.Count > seenCount)
                throw new Exception("Unknown keyword parameters."); // TODO: more proper exception
        }

        end ??= (PsString)"\n";
        sep ??= (PsString)" ";

        bool needSep = false;
        foreach (var obj in args)
        {
            if (needSep)
                Stdout.Write(sep);

            Stdout.Write(obj);

            needSep = true;
        }
        Stdout.Write(end);

        return PsConstants.None;
    }

    private PsString input(PsObject self, PsTuple args, PsDict? kwargs)
    {
        if (kwargs != null)
        {
            throw new ArgumentException("'input' function doesn't allow keyword arguments.", nameof(kwargs));
        }
        else if (args.Count > 1)
        {
            throw new ArgumentException("'input' function accepts one or zero positional arguments.", nameof(args));
        }

        if (args.Count == 1)
        {
            Stdout.Write(args[0]);
        }

        var input = Stdin.ReadLine() ?? throw new Exception("EOFError()");

        return (PsString)input;
    }
}
