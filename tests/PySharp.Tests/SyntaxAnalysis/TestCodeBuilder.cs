using PySharp.Runtime;
using PySharp.Runtime.Objects;
using PySharp.SyntaxAnalysis;
using PySharp.VM;

namespace PySharp.Tests.SyntaxAnalysis;

public class TestCodeBuilder
{
    [Fact]
    public void TestCalls() => run(b =>
    {
        var print = b.LdVar("print");
        var arg0 = b.LdConst((PsString)"Bau bau!");
        b.LdConst((PsInteger)69);
        b.Call(print.Dest!, arg0.Dest!, 2);
        b.Ret(arg0.Dest!);
    });

    private static void run(Action<CodeBuilder> codeFactory)
    {
        var builder = new CodeBuilder();
        codeFactory(builder);

        var code = builder.Dump();

        var engine = new Engine();
        var env = new Runtime.Environment();
        env.Scopes.Push(Builtins.BuiltinsScope);

        engine.RunCode(code, [], env);
    }
}
