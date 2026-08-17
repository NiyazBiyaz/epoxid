using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Common.Ast;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.Tests.SyntaxAnalysis.Common.Ast;

// Here because SyntaxAnalysis library now don't have any infrastructure for testing nodes.
// TODO: Move SyntaxAnalysis to main project (again) with making PegenNet more standalone
public class TestRedView
{
    [Fact]
    public void TestGetAssignmentStatements()
    {
        const string src = """
        b=10
        a:int=20

        if bau:
            u=30
        """;
        var view = getView(src);

        var assignments = view.Children().OfType<AssignmentView>().ToArray();

        Assert.Equal(3, assignments.Length);

        for (int i = 0; i < assignments.Length; i++)
        {
            var ass = assignments[i];

            switch (i)
            {
                case 0:
                {
                    if (ass is not SimpleAssignmentView simple)
                    {
                        Assert.Fail("Invalid node type");
                        return;
                    }

                    Assert.Equal("b", simple.Target.RawString);
                    Assert.Equal("10", simple.Rhs.Value.RecoverText());

                    break;
                }
                case 1:
                {
                    if (ass is not AnnotatedAssignmentView annotated)
                    {
                        Assert.Fail("Invalid node type");
                        return;
                    }

                    Assert.Equal("a", annotated.Target.RawString);
                    Assert.Equal("int", annotated.TypeHint.RecoverText());
                    Assert.Equal("20", annotated.Rhs!.Value.RecoverText());

                    break;
                }
                case 2:
                {
                    if (ass is not SimpleAssignmentView annotated)
                    {
                        Assert.Fail("Invalid node type");
                        return;
                    }

                    Assert.Equal("u", annotated.Target.RawString);
                    Assert.Equal("30", annotated.Rhs!.Value.RecoverText());

                    break;
                }
            }
        }
    }

    private static IRedView getView(string src)
    {
        var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(new StringBuffer(src)));
        var parser = new PythonParser(new TokenNodeStream(tokenizer));
        var node = parser.Parse();

        if (node == null)
            Assert.Fail("Given code is invalid ;O");

        var view = node.GetView(0, null);
        view.SyntaxTree = new SyntaxViewTree
        {
            Root = view,
            PositionMap = tokenizer.PositionMap,
        };

        return view;
    }
}
