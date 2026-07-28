using System.Diagnostics;
using System.Text;
using PySharp.Runtime.Objects;
using PySharp.SyntaxAnalysis;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Common.Ast;
using PySharp.SyntaxAnalysis.Tokens;

namespace PySharp.Runtime;

public partial class Interpreter
{
    private readonly Stack<Scope> scopes = [];

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
                    throw notImplemented(statement);
            }
        }
    }

    public void InterpretSimpleStatement(ISimpleStatementView simpleStatement)
    {
        switch (simpleStatement)
        {
            case AnnotatedAssignmentView annotatedAssignment:
            {
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
            }

            case SimpleAssignmentView simpleAssignment:
            {
                if (simpleAssignment.Rhs is not EqualAnnotatedRhsView rhs)
                {
                    throw notImplemented(simpleAssignment);
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

                scopes.Peek().Bind(simpleAssignment.Target.RawString, exprValue);

                break;
            }

            case StarExpressionsView starExpressionsView:
            {
                foreach (var starExpression in starExpressionsView.Values)
                {
                    if (starExpression is not IExpressionView expr)
                    {
                        throw notImplemented(starExpression);
                    }

                    InterpretExpression(expr);
                }

                break;
            }

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
            RawPrimaryView rawPrimary => evaluateRawPrimary(rawPrimary),

            SumView sum => sum.Operator.Type switch
            {
                TokenType.Plus => AddObjects(evaluateArithmetic(sum.Left), evaluateArithmetic(sum.Right)),
                TokenType.Minus => SubtractObjects(evaluateArithmetic(sum.Left), evaluateArithmetic(sum.Right)),
                _ => throw new ArgumentOutOfRangeException(),
            },

            TermView term => term.Operator.Type switch
            {
                TokenType.Star => MultiplyObjects(evaluateArithmetic(term.Left), evaluateArithmetic(term.Right)),
                TokenType.Slash => TrueDivideObjects(evaluateArithmetic(term.Left), evaluateArithmetic(term.Right)),
                TokenType.DoubleSlash => throw notImplemented(term.Operator), // Integer division
                TokenType.Percent => throw notImplemented(term.Operator), // Module
                TokenType.At => throw notImplemented(term.Operator), // Matrix multiplication
                _ => throw new ArgumentOutOfRangeException(),
            },

            PowerView pow => PowerObjects(evaluateArithmetic(pow.Left), evaluateArithmetic(pow.Right)),

            _ => throw notImplemented(arithmetic),
        };
    }

    private PsObject evaluateRawPrimary(RawPrimaryView rawPrimary)
    {
        switch (rawPrimary)
        {
            case AtomPrimaryView atomPrimary:
                return atomPrimary.Value switch
                {
                    NumberAtomView number => NumberParser.ParseNumber(number.Value.RawString),

                    NameAtomView name => getVariableFromEnvironment(name.Value.RawString)
                        ?? throw new Exception($"Variable '{name.Value.RawString}' is undefined."),

                    TrueAtomView => PsConstants.True,

                    FalseAtomView => PsConstants.False,

                    NoneAtomView => PsConstants.None,

                    EllipsisAtomView => PsConstants.Ellipsis,

                    StringAtomView str => evaluateString(str),

                    _ => throw notImplemented(atomPrimary.Value),
                };

            case CallWithArgumentsPrimaryView callPrimary:
                var func = evaluateRawPrimary(callPrimary.Function);
                var arguments = evaluateArguments(callPrimary.Arguments);
                return CallFunction(func, arguments.arguments, arguments.keywordArguments);

            default:
                throw notImplemented(rawPrimary);
        }
        ;
    }

    private PsString evaluateString(StringAtomView str)
    {
        if (str is not StringValueAtomView strAtom)
        {
            throw notImplemented(str);
        }

        StringBuilder builder = new();
        foreach (var strPart in strAtom.Parts)
        {
            if (strPart is not StringConstantView strConst)
            {
                throw notImplemented(strPart);
            }

            if (StringParser.HasPrefix(strConst.Value.RawString))
            {
                throw notImplemented(strConst);
            }

            builder.Append(StringParser.ParseQuoted(strConst.Value.RawString));
        }

        return (PsString)builder.ToString();
    }

    private (PsTuple arguments, PsDict? keywordArguments) evaluateArguments(ArgumentsView? arguments)
    {
        if (arguments == null)
        {
            return (new PsTuple([]), null);
        }

        switch (arguments)
        {
            case WithPositionalArgumentsView withPositional:
                List<PsObject> positional = [];
                foreach (var pos in withPositional.PositionalArgumentsPart)
                {
                    if (pos is not IExpressionView expression)
                        throw notImplemented(pos);

                    var value = InterpretExpression(expression);
                    positional.Add(value);
                }

                if (withPositional.KeywordArgumentsPart != null)
                {
                    throw notImplemented(withPositional.KeywordArgumentsPart);
                }

                return (new PsTuple(positional.ToArray()), null);

            default:
                throw notImplemented(arguments);
        }
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
