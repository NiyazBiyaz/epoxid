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

    private readonly Scope builtins = new();

    // It should be PyObject, but later.
    public TextWriter Stdout { get; set; } = Console.Out;
    public TextReader Stdin { get; set; } = Console.In;
    public TextWriter Stderr { get; set; } = Console.Error;

    public void InterpretFile(FileView file)
    {
        Debug.Assert(file != null);

        // Load scope for the file code-block.
        scopes.Push(new Scope());

        foreach (var statement in file.Statements)
        {
            InterpretStatement(statement);
        }
    }

    public void InterpretStatement(IStatementView statement)
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

            case IfStatementView ifStatement:
            {
                var condition = ifStatement.Condition;
                bool fold = false;

                if (fold = Core.ConvertBool(InterpretExpression(condition)))
                {
                    InterpretBlock(ifStatement.Block);
                }

                int elifIndex = 0;
                while (elifIndex < ifStatement.Elifs.Count && !fold)
                {
                    var elif = ifStatement.Elifs[elifIndex++];
                    if (fold = Core.ConvertBool(InterpretExpression(elif.Condition)))
                    {
                        InterpretBlock(elif.Block);
                    }
                }

                if (!fold && ifStatement.Else != null)
                {
                    InterpretBlock(ifStatement.Else.Block);
                }

                break;
            }

            case WhileStatementView whileStatement:
            {
                var condition = whileStatement.Condition;
                scopes.Peek().SetupLoop();
                while (Core.ConvertBool(InterpretExpression(condition)) && !scopes.Peek().PeekLoop())
                {
                    InterpretBlock(whileStatement.Block);
                }

                if (!scopes.Peek().TerminateLoop() && whileStatement.Else != null)
                {
                    InterpretBlock(whileStatement.Else.Block);
                }

                break;
            }

            case FunctionDefView:
            case ForStatementView:
            case WithStatementView:
            case ClassDefView:
            case TryStatementView:
            default:
                throw notImplemented(statement);
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

                if (rhs.Value is not IExpressionView expr)
                {
                    throw notImplemented(rhs.Value);
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

                if (rhs.Value is not IExpressionView expr)
                {
                    throw notImplemented(rhs.Value);
                }

                PsObject exprValue = InterpretExpression(expr);

                scopes.Peek().Bind(simpleAssignment.Target.RawString, exprValue);

                break;
            }

            case IExpressionView expr:
            {
                InterpretExpression(expr);
                break;
            }

            case BreakStatementView:
            {
                scopes.Peek().SetBreakFlag();
                break;
            }

            case AugmentedAssignmentView:
            case IStarExpressionVariantView:
            case CascadeAssignmentView:
            case GlobalStatementView:
            case AssertStatementView:
            case NonlocalStatementView:
            case TypeAliasView:
            case YieldStatementView:
            case RaiseStatementView:
            case ContinueStatementView:
            case DeleteStatementView:
            case ReturnStatementView:
            case AnnotatedParenthesizedAssignmentView:
            case PassStatementView:
            case AnnotatedSubscriptAttributeAssignmentView:
            case IImportStatementView:
                throw notImplemented(simpleStatement);
            default:
                throw new InvalidOperationException();
        }
    }

    public PsObject InterpretExpression(INamedExpressionView namedExpr)
    {
        switch (namedExpr)
        {
            case IExpressionView expr:
                return expr switch
                {
                    IBitwiseOrExpressionView arithmetic => evaluateArithmetic(arithmetic),

                    // TODO: change grammar or use proper ast walking
                    DisjunctionView disjunction => disjunction.Values.First().Values.First() switch
                    {
                        ComparisonView comparison => evaluateComparison(comparison),

                        InversionView inversion => throw notImplemented(inversion),

                        _ => throw new InvalidOperationException(),
                    },

                    IfExpressionView view => throw notImplemented(view),

                    _ => throw notImplemented(expr),
                };

            case AssignmentExpressionView assignmentExpression:
                var value = InterpretExpression(assignmentExpression.Value);
                scopes.Peek().Bind(assignmentExpression.Target.RawString, value);
                return value;

            default:
                throw new ArgumentException("Wrong expression type", nameof(namedExpr));
        }
    }

    public void InterpretBlock(BlockView block)
    {
        switch (block)
        {
            case OneLinedBlockView oneLined:
                InterpretStatement(oneLined.Statements);
                break;

            case IndentedBlockView indented:
                foreach (var stmt in indented.Statements)
                    InterpretStatement(stmt);

                break;
        }
    }

    private PsObject evaluateComparison(ComparisonView comparison)
    {
        PsObject left = evaluateArithmetic(comparison.First), right;

        bool fold = true;

        foreach (var value in comparison.Rest)
        {
            right = evaluateArithmetic(value switch
            {
                // Maybe extend by interface?
                NotInOperationView view => view.Right,
                EqOperationView view => view.Right,
                GtOperationView view => view.Right,
                LtOperationView view => view.Right,
                GtEqOperationView view => view.Right,
                NotEqOperationView view => view.Right,
                IsOperationView view => view.Right,
                InOperationView view => view.Right,
                IsNotOperationView view => view.Right,
                LtEqOperationView view => view.Right,
                _ => throw new InvalidOperationException(),
            });

            PsObject result = value switch
            {
                EqOperationView => Core.EqualObjects(left, right),
                NotEqOperationView => Core.NotEqualObjects(left, right),

                NotInOperationView view => throw notImplemented(view),
                GtOperationView view => throw notImplemented(view),
                LtOperationView view => throw notImplemented(view),
                GtEqOperationView view => throw notImplemented(view),
                IsOperationView view => throw notImplemented(view),
                InOperationView view => throw notImplemented(view),
                IsNotOperationView view => throw notImplemented(view),
                LtEqOperationView view => throw notImplemented(view),
                _ => throw new InvalidOperationException(),
            };

            fold &= Core.ConvertBool(result);

            if (!fold)
                break;

            left = right;
        }

        return (PsBool)fold;
    }

    private PsObject evaluateArithmetic(IBitwiseOrExpressionView arithmetic)
    {
        return arithmetic switch
        {
            RawPrimaryView rawPrimary => evaluateRawPrimary(rawPrimary),

            SumView sum => sum.Operator.Type switch
            {
                TokenType.Plus => Core.AddObjects(evaluateArithmetic(sum.Left), evaluateArithmetic(sum.Right)),
                TokenType.Minus => Core.SubtractObjects(evaluateArithmetic(sum.Left), evaluateArithmetic(sum.Right)),
                _ => throw new ArgumentOutOfRangeException(),
            },

            TermView term => term.Operator.Type switch
            {
                TokenType.Star => Core.MultiplyObjects(evaluateArithmetic(term.Left), evaluateArithmetic(term.Right)),
                TokenType.Slash => Core.TrueDivideObjects(evaluateArithmetic(term.Left), evaluateArithmetic(term.Right)),
                TokenType.DoubleSlash => throw notImplemented(term.Operator), // Integer division
                TokenType.Percent => throw notImplemented(term.Operator), // Module
                TokenType.At => throw notImplemented(term.Operator), // Matrix multiplication
                _ => throw new ArgumentOutOfRangeException(),
            },

            PowerView pow => Core.PowerObjects(evaluateArithmetic(pow.Left), evaluateArithmetic(pow.Right)),

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
                    NumberAtomView number => NumberParser.GetNumberType(number.Value.RawString) switch
                    {
                        NumberType.Integer => (PsInteger)NumberParser.ParseInteger(number.Value.RawString),
                        NumberType.Float => (PsFloat)NumberParser.ParseFloat(number.Value.RawString),
                        NumberType.Complex => throw new NotImplementedException(), //(PsComplex)NumberParser.ParseComplex(number.Value.RawString),
                        _ => throw new UnreachableException(),
                    },

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
                return Core.CallFunction(func, arguments.arguments, arguments.keywordArguments);

            default:
                throw notImplemented(rawPrimary);
        }
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

    private (PsTuple arguments, PsDict keywordArguments) evaluateArguments(ArgumentsView? arguments)
    {
        if (arguments == null)
        {
            return ([], []);
        }

        switch (arguments)
        {
            case ArgumentsWithPositionalView withPositional:
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

                return ([.. positional], []);

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
        $"Error {view.Position2D}-{view.EndPosition2D}: This instruction ({view.GetType().Name}) is not supported yet.");
}
