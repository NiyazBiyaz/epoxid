using PySharp.Runtime;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;

namespace PySharp.Tests;

public class TestInterpreter
{
    [Fact]
    public void TestPrintFunction()
    {
        const string src = """
        print("bau bau!")

        """;
        assertOutput(src, "bau bau!");
    }

    [Fact]
    public void Test_VariablesAndPrimitiveArithmetic()
    {
        const string src = """
        fluffy = 35
        fuzzy = 34
        bauBau = fluffy + fuzzy
        ponDeRing = bauBau / 3
        fwmc = ponDeRing * 2
        print(bauBau, ponDeRing, fwmc, 69 - 17)

        """;
        assertOutput(src, "69 23.0 46.0 52");
    }

    [Fact]
    public void Test_PrimitiveStringOperations()
    {
        const string src = """
        bau = "Bau"
        bauBauBau = bau * 3
        waga = "w" + "aga"
        print(bauBauBau, waga)

        """;
        assertOutput(src, "BauBauBau waga");
    }

    [Fact]
    public void TestInputFunction()
    {
        const string src = """
        bau = input()
        print(bau)

        """;
        assertForInput(src, "Bau bau!", "Bau bau!");
    }

    [Fact]
    public void TestInputFunction_WithHint()
    {
        const string src = """
        bau = input("Baau baaau")
        print(bau)

        """;
        assertForInput(src, "Baau baaauBau bau!", "Bau bau!");
    }

    [Theory]
    [InlineData("yes", "bau bau!")]
    [InlineData("nah", "Hoeh? B-but... BAU BAU!")]
    public void Test_IfElseStatement(string answer, string action)
    {
        const string src = """
        doBauBau = input("Do you want bau bau? ") == "yes"

        if doBauBau:
            print("bau bau!")
        else:
            print("Hoeh? B-but... BAU BAU!")

        """;
        assertForInput(src, "Do you want bau bau? " + action, answer);
    }

    [Fact]
    public void Test_BooleanConversion()
    {
        const string src = """
        b = 1 + 2
        if b:
            print("bau")

        a = ""
        if a:
            print("baubau")

        u = 3.0
        if u - b:
            print("baubaubau")

        """;
        assertOutput(src, "bau");
    }

    [Fact]
    public void Test_IntegerEquality()
    {
        const string src = """
        a = 123
        b = 123
        print(a == b)

        """;
        assertOutput(src, "True");
    }

    [Fact]
    public void Test_WhileAndBreak_WhenElse()
    {
        const string src = """
        a = 10
        while a != 0:
            print(a, "bau bau!")
            if a == 5:
                break
            a = a - 1
        else:
            print("another bau bau!")

        """;
        const string output =
        """
        10 bau bau!
        9 bau bau!
        8 bau bau!
        7 bau bau!
        6 bau bau!
        5 bau bau!
        """;
        assertOutput(src, output);
    }

    [Fact]
    public void Test_WhileAndBreak()
    {
        const string src = """
        a = 10
        while a != 0:
            print(a, "bau bau!")
            if a == 5:
                break
            a = a - 1

        """;
        const string output =
        """
        10 bau bau!
        9 bau bau!
        8 bau bau!
        7 bau bau!
        6 bau bau!
        5 bau bau!
        """;
        assertOutput(src, output);
    }

    [Fact]
    public void Test_WhileLoop()
    {
        const string src = """
        a = 10
        while a != 4:
            print(a, "bau bau!")
            a = a - 1

        """;
        const string output =
        """
        10 bau bau!
        9 bau bau!
        8 bau bau!
        7 bau bau!
        6 bau bau!
        5 bau bau!
        """;
        assertOutput(src, output);
    }

    private static void assertOutput(string src, string expected, bool includeNewLine = true)
    {
        var file = getView(src);
        var interpreter = getInterpreterStdout(out var stdout);

        interpreter.InterpretFile(file);

        Assert.Equal(expected + (includeNewLine ? "\n" : ""), stdout.ToString());
    }

    private static void assertForInput(string src, string expected, string input, bool includeNewLine = true)
    {
        var file = getView(src);
        var interpreter = getInterpreterStdoutWithStdin(out var stdout, input + (includeNewLine ? "\n" : ""));

        interpreter.InterpretFile(file);

        Assert.Equal(expected + (includeNewLine ? "\n" : ""), stdout.ToString());
    }

    private static FileView getView(string src)
    {
        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));
        var parser = new PythonParser(new TokenNodeStream(tokenizer));
        var node = parser.Parse();

        if (node == null)
        {
            Assert.Fail("Given code is invalid");
        }

        var view = node.GetView(0, null);
        view.SyntaxTree = new SyntaxViewTree
        {
            Root = view,
            PositionMap = tokenizer.PositionMap,
        };

        return view;
    }

    private static Interpreter getInterpreter()
    {
        var inter = new Interpreter();
        inter.LoadBuiltins();
        return inter;
    }

    private static Interpreter getInterpreterStdout(out StringWriter stdout)
    {
        var inter = getInterpreter();
        inter.Stdout = stdout = new StringWriter();
        return inter;
    }

    private static Interpreter getInterpreterStdoutWithStdin(out StringWriter stdout, string input)
    {
        var inter = getInterpreter();
        inter.Stdout = stdout = new StringWriter();
        inter.Stdin = new StringReader(input);
        return inter;
    }
}
