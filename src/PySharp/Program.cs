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
        var engine = new Engine();

        var builder = new CodeBuilder();

        var left = builder.LdConst((PsString)"bau ");
        var right = builder.LdConst((PsInteger)5);
        var res = builder.RegisterToRegister(Opcode.Mul, left.Dest!, right.Dest!);

        var print = builder.LdVar("print");
        var arg0 = builder.LdConst((PsString)"Bau bau!");
        builder.LdConst((PsInteger)69);
        builder.Move(res.Dest!);
        builder.Call(print.Dest!, arg0.Dest!, 3);
        builder.Ret(arg0.Dest!);

        var code = builder.Dump();

        var env = new Runtime.Environment();
        env.Scopes.Push(Builtins.BuiltinsScope);

        engine.RunCode(code, [], env);
    }
}
