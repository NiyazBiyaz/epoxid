using System.Diagnostics;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.SyntaxAnalysis.Generator.Tests;

// I wanted to put this somewhere suitable, but the only working parser for testing Views is here.
// Anyway PegenNet might come more independent project and test it also matters.
public class TestParser
{
    [Fact]
    public void TestViewPositioning_PlainPositions()
    {
        const string src = """
        @header "using BauBau.Mindset;"
        @parser_name "PonDeRingParser"

        """;
        var view = getView(src);

        Assert.Equal(00, view.Metadata[0].FullPosition);
        Assert.Equal(32, view.Metadata[0].EndPosition);
        Assert.Equal(32, view.Metadata[0].EndPosition - view.Metadata[0].FullPosition);
        Assert.Equal(32, view.Metadata[1].FullPosition);
        Assert.Equal(63, view.Metadata[1].EndPosition);
        Assert.Equal(31, view.Metadata[1].EndPosition - view.Metadata[1].FullPosition);
    }

    [Fact]
    public void TestViewPositioning_2DPositions()
    {
        const string src = """
        @header "using BauBau.Mindset;"
        @parser_name "PonDeRingParser"

        @main
        FuwaMoco:
            | "bau" "bau" ["lowercase"]
            | "BAU" "BAU" &"UPPERCASE"

        """;
        var view = getView(src);

        Assert.Equal(0, view.Metadata[0].StartLocation.Line);
        Assert.Equal(1, view.Metadata[0].EndLocation.Line);
        Assert.Equal(1, view.Metadata[1].StartLocation.Line);
        Assert.Equal(2, view.Metadata[1].EndLocation.Line);

        Assert.Equal(3, view.Rules[0].Decorators[0].StartLocation.Line);
        Assert.Equal(4, view.Rules[0].Decorators[0].EndLocation.Line);

        Assert.Equal(3, view.Rules[0].StartLocation.Line);
        Assert.Equal(7, view.Rules[0].EndLocation.Line);

        Assert.Equal(5, (view.Rules[0] as ArmedRuleView)!.Arms[0].StartLocation.Line);
        Assert.Equal(4, (view.Rules[0] as ArmedRuleView)!.Arms[0].StartLocation.Column);
        Assert.Equal(6, (view.Rules[0] as ArmedRuleView)!.Arms[0].EndLocation.Line);
        Assert.Equal(0, (view.Rules[0] as ArmedRuleView)!.Arms[0].EndLocation.Column);
    }

    private static GrammarView getView(string src)
    {
        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));
        var tokenStream = new TokenNodeStream(tokenizer);
        var parser = new GrammarParser(tokenStream);
        var grammar = parser.Parse();
        Debug.Assert(grammar != null, "Invalid test code");
        var view = grammar.GetView(0, null);
        view.SyntaxTree = new SyntaxViewTree
        {
            PositionMap = tokenizer.PositionMap,
            Root = view,
        };
        return view;
    }
}
