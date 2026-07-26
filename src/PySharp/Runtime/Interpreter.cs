using System.Diagnostics;
using PySharp.Runtime.Objects;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Common.Ast;

namespace PySharp.Runtime;

public class Interpreter
{
    private readonly Stack<Scope> scopes = [];

    public void LoadInterpreter()
    {
        var builtins = new Scope();

        scopes.Push(builtins);
    }

    public void InterpretFile(FileView file)
    {
        Debug.Assert(file != null);

        // Load scope for the file code-block.
        scopes.Push(new Scope());

        foreach (var statement in file.Statements)
        {
            switch (statement)
            {
                case SingleSimpleStatementView singleStatement:
                    InterpretSimpleStatement(singleStatement.Value);
                    break;

                case SeparatedSimpleStatementsView simpleStatements:
                    foreach (var stmt in simpleStatements.Values)
                        InterpretSimpleStatement(stmt);

                    break;

                default:
                    notImplemented(statement);
                    break;
            }
        }
    }

    public void InterpretSimpleStatement(ISimpleStatementView simpleStatement)
    {
        switch (simpleStatement)
        {
            case AnnotatedAssignmentView annotatedAssignment:
                if (annotatedAssignment.Rhs is not EqualAnnotatedRhsView rhs)
                {
                    throw notImplemented(annotatedAssignment);
                }

                if (rhs.Value is not StarExpressionsView starExpressions)
                {
                    throw notImplemented(rhs.Value);
                }
                if (starExpressions.Values.Length != 1)
                {
                    throw notImplemented(starExpressions.AstValues);
                }

                var starExpressionOrExpression = starExpressions.Values[0];

                if (starExpressionOrExpression is not IExpressionView expr)
                {
                    throw notImplemented(starExpressionOrExpression);
                }

                PsObject exprValue = InterpretExpression(expr);

                scopes.Peek().Bind(annotatedAssignment.Target.RawString, exprValue);

                break;

            default:
                throw notImplemented(simpleStatement);
        }
    }

    public PsObject InterpretExpression(IExpressionView expr)
    {
        return expr switch
        {
            IBitwiseOrExpressionView arithmetic => evaluateArithmetic(arithmetic),
            _ => throw notImplemented(expr),
        };
    }

    private PsObject evaluateArithmetic(IBitwiseOrExpressionView arithmetic)
    {
        return arithmetic switch
        {
            AtomPrimaryView atomPrimary => atomPrimary.Value switch
            {
                NumberAtomView number => NumberParser.ParseNumber(number.Value.RawString),

                NameAtomView name => getVariableFromEnvironment(name.Value.RawString)
                    ?? throw new Exception($"Variable '{name.Value.RawString}' is undefined."),

                TrueAtomView => PsConstants.True,

                FalseAtomView => PsConstants.False,

                NoneAtomView => PsConstants.None,

                EllipsisAtomView => PsConstants.Ellipsis,

                _ => throw notImplemented(atomPrimary.Value),
            },
            _ => throw notImplemented(arithmetic),
        };
    }

    private PsObject? getVariableFromEnvironment(ReadOnlySpan<char> variableName)
    {
        foreach (var scope in scopes)
        {
            if (scope.TryGetValue(variableName, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static NotImplementedException notImplemented(IRedView view) => new NotImplementedException(
        $"Error {view.Position2D}-{view.EndPosition2D}: This instruction is not supported yet.");
}
