using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Epoxid.Runtime.Objects;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;
using Epoxid.VM;

namespace Epoxid.SyntaxAnalysis;

// TODO: class validator

/// <summary>
/// Class to bind names of the <i>code block</i> and validate it.
/// <br/><br/>
/// <seealso href="https://docs.python.org/3/reference/executionmodel.html#structure-of-a-program"/>
/// </summary>
internal class CodeBlockGenerator(IEnumerable<IStatementView> statements)
{
    public List<string> Variables { get; init; } = [];

    public CodeBuilder Builder { get; } = new();

    private readonly Dictionary<string, Register> locals = [];

    private readonly IEnumerable<IStatementView> blockStatements = statements;

    public ValidationResult GenerateCode() => validateStatements(blockStatements);

    private ValidationResult validateStatements(IEnumerable<IStatementView> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case IfStatementView ifStmt:

                    break;

                case ClassDefView:
                case ForStatementView:
                case FunctionDefView:
                case WhileStatementView:
                case TryStatementView:
                case WithStatementView:
                    throw new NotImplementedException();

                case SeparatedSimpleStatementsView separated:
                    foreach (var stmt in separated.Values)
                    {
                        generateSimpleStatement(stmt);
                    }
                    break;

                case SingleSimpleStatementView single:
                {
                    generateSimpleStatement(single.Value);
                    break;
                }

                default:
                    throw new UnreachableException();
            }
        }

        Builder.RetC(EpConstants.None);

        return ValidationResult.ResultSuccess;
    }

    private IntermediateInstruction generateSimpleStatement(ISimpleStatementView statement)
    {
        IntermediateInstruction label;
        switch (statement)
        {
            case AssignmentView assignment:
                label = generateAssignment(assignment);
                break;

            case IStarExpressionVariantView expr:
                label = generateExpression(expr);
                break;

            case ContinueStatementView:
            case BreakStatementView:
            case TypeAliasView:
            case YieldStatementView:
            case RaiseStatementView:
            case GlobalStatementView:
            case NonlocalStatementView:
            case AssertStatementView:
            case IImportStatementView:
            case DeleteStatementView:
            case ReturnStatementView:
            case PassStatementView:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }

        return label;
    }

    private IntermediateInstruction generateExpression(IStarExpressionVariantView starExpression)
    {
        IntermediateInstruction label;

        switch (starExpression)
        {
            case IBitwiseOrExpressionView arithmetic:
            {
                label = generateArithmetic(arithmetic);
                break;
            }

            case DisjunctionView disjunction:
            case IfExpressionView ifExpression:
            case StarBitwiseOrExpressionView:
            case StarExpressionsView:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }

        return label;
    }

    private IntermediateInstruction generateArithmetic(IBitwiseOrExpressionView arithmetic)
    {
        switch (arithmetic)
        {
            case BitwiseOrView or:
            case BitwiseXorView xor:
            case BitwiseAndView and:
            case BitShiftView bitShift:
                throw new NotImplementedException();

            case SumView sum:
            {
                IntermediateInstruction? label = null;
                if (ensureExpressionRegister(sum.Left, out Register leftReg, out var leftLabel))
                {
                    label ??= leftLabel;
                }
                if (ensureExpressionRegister(sum.Right, out Register rightReg, out var rightLabel))
                {
                    label ??= rightLabel;
                }

                var sumLabel = Builder.RegisterToRegister(sum.Operator.Type switch
                {
                    TokenType.Plus => Opcode.Add,
                    TokenType.Minus => Opcode.Sub,
                    _ => throw new UnreachableException()
                },
                leftReg,
                rightReg);

                label ??= sumLabel;

                return sumLabel;
            }

            case TermView term:
            {
                IntermediateInstruction? label = null;
                if (ensureExpressionRegister(term.Left, out Register leftReg, out var leftLabel))
                {
                    label ??= leftLabel;
                }
                if (ensureExpressionRegister(term.Right, out Register rightReg, out var rightLabel))
                {
                    label ??= rightLabel;
                }

                var termLabel = Builder.RegisterToRegister(term.Operator.Type switch
                {
                    TokenType.Star => Opcode.Mul,
                    TokenType.Slash => Opcode.TDiv,
                    TokenType.Percent => throw new NotImplementedException(),
                    TokenType.DoubleSlash => throw new NotImplementedException(),
                    TokenType.At => throw new NotImplementedException(),
                    _ => throw new UnreachableException(),
                },
                leftReg,
                rightReg);

                label ??= termLabel;

                return label;
            }

            case FactorView factor:
            case PowerView power:
            case AwaitPrimaryView awaitPrimary:
            case DotOperationPrimaryView dotPrimary:
            case SubscriptPrimaryView subscript:
            case CallWithGeneratorPrimaryView callGenerator:
                throw new NotImplementedException();

            case CallWithArgumentsPrimaryView call:
                return generateCall(call);

            case AtomPrimaryView atom:
                return atom.Atom switch
                {
                    NumberAtomView number => loadNumber(number.Value.RawString),

                    NameAtomView var => locals.ContainsKey(var.Value.RawString)
                        ? throw new InvalidGeneratorException($"Use '{nameof(ensureVariableRegister)}()' to access already loaded variables")
                        : Builder.LdVar(var.Value.RawString),

                    StringValueAtomView str => loadString(str),

                    StringTemplateAtomView tStr => throw new NotImplementedException(),

                    NoneAtomView => Builder.LdConst(EpConstants.None),

                    TrueAtomView => Builder.LdConst(EpConstants.True),

                    FalseAtomView => Builder.LdConst(EpConstants.False),

                    EllipsisAtomView => Builder.LdConst(EpConstants.Ellipsis),

                    GroupView view => throw new NotImplementedException(),
                    TupleView view => throw new NotImplementedException(),
                    ListComprehensionView view => throw new NotImplementedException(),
                    SetView view => throw new NotImplementedException(),
                    DictView view => throw new NotImplementedException(),
                    SetComprehensionView view => throw new NotImplementedException(),
                    DictComprehensionView view => throw new NotImplementedException(),
                    ListView view => throw new NotImplementedException(),
                    GeneratorExpressionView view => throw new NotImplementedException(),

                    _ => throw new UnreachableException()
                };

            default:
                throw new UnreachableException();
        }
    }

    private IntermediateInstruction loadNumber(string numberString)
    {
        var type = NumberParser.GetNumberType(numberString);

        return Builder.LdConst(type switch
        {
            NumberType.Integer => (EpInteger)NumberParser.ParseInteger(numberString),
            NumberType.Float => (EpFloat)NumberParser.ParseFloat(numberString),
            NumberType.Complex => throw new NotImplementedException(), //(PsComplex)NumberParser.ParseComplex(numberString),
            _ => throw new UnreachableException(),
        });
    }

    private IntermediateInstruction loadString(StringValueAtomView str)
    {
        if (str.Parts.Any(p => p is FStringView))
            throw new NotImplementedException();

        string strValue;
        switch (str.Parts.Count)
        {
            case 1:
                strValue = ((StringConstantView)str.Parts[0]).Value.RawString;
                strValue = StringParser.ParseQuoted(strValue);
                break;

            case 2:
                strValue = ((StringConstantView)str.Parts[0]).Value.RawString + ((StringConstantView)str.Parts[0]).Value.RawString;
                break;

            default:
                var sb = new StringBuilder();

                foreach (var part in str.Parts)
                    sb.Append(((StringConstantView)part).Value.RawString);

                strValue = sb.ToString();
                break;
        }

        return Builder.LdConst((EpString)strValue);
    }

    private IntermediateInstruction generateAssignment(AssignmentView assignment)
    {
        IntermediateInstruction? label;

        switch (assignment)
        {
            case SimpleAssignmentView simple:
                switch (simple.Rhs.Value)
                {
                    case IStarExpressionVariantView expr:
                    {
                        ensureExpressionRegister(expr, out var dest, out label);

                        var name = simple.Target.RawString;
                        // TODO: find assignment variables before generating actual bytecode.
                        if (locals.TryGetValue(name, out var register))
                        {
                            Builder.Move(register, dest);
                        }
                        else
                        {
                            locals[name] = dest;
                        }

                        break;
                    }

                    case YieldExpressionView:
                        throw new NotImplementedException();

                    default:
                        throw new UnreachableException();
                }
                break;

            case AnnotatedSubscriptAttributeAssignmentView:
            case AnnotatedParenthesizedAssignmentView:
            case AugmentedAssignmentView:
            case CascadeAssignmentView:
            case AnnotatedAssignmentView:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }

        // TODO: remove this 'label' pattern and use real data structure for labels
        return label!;
    }

    private IntermediateInstruction generateCall(CallWithArgumentsPrimaryView call)
    {
        Register[] argumentRegisters;
        IntermediateInstruction? label = null;
        // TODO: keyword arguments
        // Computing all argument expressions and getting their registers
        switch (call.Arguments)
        {
            case ArgumentsWithPositionalView args:
            {
                // TODO: star-unpacking
                var positionalArgs = args.PositionalArgumentsPart.OfType<INamedExpressionView>().ToImmutableArray();
                argumentRegisters = new Register[positionalArgs.Length];

                for (int i = 0; i < positionalArgs.Length; i++)
                {
                    switch (positionalArgs[i])
                    {
                        case IExpressionView expr:
                            if (ensureExpressionRegister(expr, out argumentRegisters[i], out var instruction))
                                label ??= instruction;
                            break;

                        case AssignmentExpressionView assignment:
                            throw new NotImplementedException();

                        default:
                            throw new UnreachableException();
                    }
                }

                break;
            }

            case ArgumentsWithOnlyKeywordsView:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }

        // Putting function to frame
        if (ensureExpressionRegister(call.Function, out var funcRegister, out var funcInstruction))
            label ??= funcInstruction;
        else
            label ??= Builder.Move(funcRegister);

        Register? firstArgument = null;
        // Filling registers with the arguments
        if (argumentRegisters.Length != 0)
        {
            foreach (var argumentReg in argumentRegisters)
            {
                var move = Builder.Move(argumentReg);
                firstArgument ??= move.Dest!;
            }
        }
        // If arguments was not set, load 'None' next to the function to make it return in it
        else
        {
            firstArgument = Builder.LdConst(EpConstants.None).Dest!;
        }

        Builder.Call(funcRegister, firstArgument!, argumentRegisters.Length);

        return label;
    }

    /// <summary>
    /// Checks that variable was loaded to the frame and generates load instruction if it wasn't.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if method generated new instruction, otherwise <see langword="false"/>
    /// </returns>
    private bool ensureVariableRegister(string variableName, [NotNull] out Register? variableRegister, [NotNullWhen(true)] out IntermediateInstruction? loadInstruction)
    {
        if (locals.TryGetValue(variableName, out variableRegister))
        {
            loadInstruction = null;
            return false;
        }

        loadInstruction = Builder.LdVar(variableName);
        locals.Add(variableName, variableRegister = loadInstruction.Dest!);

        return true;
    }

    private bool ensureExpressionRegister(IStarExpressionVariantView expression, [NotNull] out Register? register, [NotNullWhen(true)] out IntermediateInstruction? instruction)
    {
        if (expression is AtomPrimaryView atom && atom.Atom is NameAtomView name)
        {
            return ensureVariableRegister(name.Value.RawString, out register, out instruction);
        }

        instruction = generateExpression(expression);
        register = instruction.Dest!;
        return true;
    }
}

internal class InvalidGeneratorException(string? message) : InvalidOperationException(message);
