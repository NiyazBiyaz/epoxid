using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Epoxid.Runtime.Objects;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Common.Ast;
using Epoxid.SyntaxAnalysis.Tokens;
using Epoxid.VM;

namespace Epoxid.CodeGenV2;

internal class BlockGenerator
{
    private readonly Stack<IntermediateLoop> loops = [];
    private readonly Dictionary<string, Variable> variables = [];

    private bool inLoop => loops.Count > 0;

    private readonly IEnumerable<IStatementView> blockStatements;

    public BlockGenerator(FileView file)
    {
        blockStatements = file.Statements;

        scanVariables(true);
    }

    public BlockGenerator(BlockView block)
    {
        blockStatements = block.GetStatements();

        scanVariables(false);
    }

    private void scanVariables(bool initGlobal)
    {
        var topLevelStmts = blockStatements.GetTopLevelStatements().ToImmutableArray();

        var locallyAssigned = topLevelStmts
            .SelectMany(stmt => stmt.ChildrenAndSelf())
            .SelectMany<IRedView, string>(ast => ast switch
            {
                AssignmentExpressionView assignmentExpression => [assignmentExpression.Target.RawString],
                AssignmentView assignment => assignment switch
                {
                    SimpleAssignmentView simple => [simple.Target.RawString],
                    AnnotatedParenthesizedAssignmentView view => throw new NotImplementedException(),
                    CascadeAssignmentView view => throw new NotImplementedException(),
                    AnnotatedAssignmentView view => throw new NotImplementedException(),
                    AugmentedAssignmentView view => throw new NotImplementedException(),
                    AnnotatedSubscriptAttributeAssignmentView view => throw new NotImplementedException(),
                    _ => throw new UnreachableException(),
                },

                FunctionDefView func => [func.FunctionDef.Name.RawString],

                ClassDefView classDef => [classDef.Name.RawString],

                _ => [],
            })
            .ToHashSet();

        foreach (var name in locallyAssigned)
        {
            var variable = new Variable()
            {
                Name = name,
                Kind = initGlobal ? VariableKind.Global : VariableKind.Local,
            };
            variables.Add(name, variable);
        }

        foreach (var funcOrClassDef in topLevelStmts)
        {
            if (funcOrClassDef is FunctionDefView funcDef)
            {
                // TODO
            }
            else if (funcOrClassDef is ClassDefView classDef)
            {
                // TODO
            }
        }
    }

    public void GenerateCode(CodeBuilder builder)
    {
        foreach (var variable in variables.Values)
        {
            if (variable.Kind == VariableKind.Local)
            {
                variable.Register = builder.AllocateRegister();
            }
        }

        generateStatements(builder, blockStatements);
        builder.RetC(CodeBuilder.NoneConstantIndex);
    }

    private void generateStatements(CodeBuilder builder, IEnumerable<IStatementView> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case SingleSimpleStatementView singleSimple:
                {
                    generateSimpleStatement(builder, singleSimple.Value);
                    break;
                }
                case SeparatedSimpleStatementsView separatedSimples:
                {
                    foreach (var simple in separatedSimples.Values)
                    {
                        generateSimpleStatement(builder, simple);
                    }
                    break;
                }
                case IfStatementView ifStmt:
                {
                    var elseLabel = new Label();
                    var endLabel = new Label();

                    var conditionRegister = getExpressionRegister(builder, ifStmt.Condition);
                    builder.BrFl(elseLabel, conditionRegister);
                    generateStatements(builder, ifStmt.Block.GetStatements());
                    builder.Brc(endLabel);

                    foreach (var elif in ifStmt.Elifs)
                    {
                        builder.PutLabel(elseLabel);
                        elseLabel = new Label();
                        conditionRegister = getExpressionRegister(builder, elif.Condition);
                        builder.BrFl(elseLabel, conditionRegister);
                        generateStatements(builder, elif.Block.GetStatements());
                        builder.Brc(endLabel);
                    }

                    builder.PutLabel(elseLabel);
                    if (ifStmt.Else != null)
                    {
                        generateStatements(builder, ifStmt.Else.Block.GetStatements());
                    }

                    builder.PutLabel(endLabel);

                    break;
                }
                case WhileStatementView whileStmt:
                {
                    var headLabel = new Label();
                    var elseLabel = new Label();
                    var endLabel = new Label();
                    beginLoop(builder, new IntermediateLoop(headLabel, endLabel));

                    builder.PutLabel(headLabel);
                    var conditionRegister = getExpressionRegister(builder, whileStmt.Condition);
                    builder.BrFl(elseLabel, conditionRegister);

                    generateStatements(builder, whileStmt.Block.GetStatements());

                    endLoop(builder);

                    builder.Brc(headLabel, true);

                    builder.PutLabel(elseLabel);
                    if (whileStmt.Else != null)
                    {
                        generateStatements(builder, whileStmt.Else.Block.GetStatements());
                    }

                    builder.PutLabel(endLabel);

                    break;
                }
                case ClassDefView:
                case ForStatementView:
                case TryStatementView:
                case FunctionDefView:
                case WithStatementView:
                    throw new NotImplementedException();

                default:
                    throw new UnreachableException();
            }
        }
    }

    private void generateSimpleStatement(CodeBuilder builder, ISimpleStatementView statement)
    {
        switch (statement)
        {
            case SimpleAssignmentView simpleAssignment:
            {
                if (simpleAssignment.Rhs.Value is not IExpressionView expression)
                {
                    throw new NotImplementedException();
                }

                var variable = variables[simpleAssignment.Target.RawString];

                var result = getExpressionRegister(builder, expression, variable.Register);
                storeVariable(builder, variable, result);

                break;
            }

            case BreakStatementView:
            {
                if (!loops.TryPeek(out var loop))
                {
                    throw new Exception("Invalid program: 'break' outside of loop");
                }

                builder.Brc(loop.EndLabel);

                break;
            }
            case ContinueStatementView:
            {
                if (!loops.TryPeek(out var loop))
                {
                    throw new Exception("Invalid program: 'continue' outside of loop");
                }

                builder.Brc(loop.HeadLabel, true);

                break;
            }
            case IExpressionView expression:
                getExpressionRegister(builder, expression);
                break;

            case PassStatementView:
            {
                break;
            }
            case IImportStatementView:
            case AnnotatedSubscriptAttributeAssignmentView:
            case AugmentedAssignmentView:
            case AnnotatedParenthesizedAssignmentView:
            case AnnotatedAssignmentView:
            case CascadeAssignmentView:
            case YieldStatementView:
            case TypeAliasView:
            case GlobalStatementView:
            case AssertStatementView:
            case ReturnStatementView:
            case NonlocalStatementView:
            case RaiseStatementView:
            case DeleteStatementView:
            case StarExpressionsView:
            case StarBitwiseOrExpressionView:
                throw new NotImplementedException();
            default:
                throw new UnreachableException();
        }
    }

    private Register getExpressionRegister(CodeBuilder builder, INamedExpressionView expression, Register? resultRegister = null)
    {
        resultRegister ??= builder.AllocateRegister();

        switch (expression)
        {
            case AssignmentExpressionView assignment:
            {
                var variable = variables[assignment.Target.RawString];
                var result = getExpressionRegister(builder, assignment.Value, variable.Register);
                storeVariable(builder, variable, result);
                break;
            }
            case IfExpressionView ifExpression:
            {
                var elseLabel = new Label();
                var endLabel = new Label();

                var conditionRegister = getExpressionRegister(builder, ifExpression.Condition);
                builder.BrFl(elseLabel, conditionRegister);
                resultRegister = getExpressionRegister(builder, ifExpression.Then);
                builder.Brc(endLabel);

                builder.PutLabel(elseLabel);
                var elseResultRegister = getExpressionRegister(builder, ifExpression.Else);
                builder.Move(resultRegister, elseResultRegister);

                builder.PutLabel(endLabel);

                break;
            }
            case DisjunctionView disjunction:
            {
                var shortCircuit = new Label();
                foreach (var conj in disjunction.Conjunctions)
                {
                    var expressionRegister = generateConjunction(builder, conj);
                    builder.Move(resultRegister, expressionRegister);
                    builder.BrTr(shortCircuit, resultRegister);
                }
                builder.PutLabel(shortCircuit);
                break;
            }
            case BitwiseOrView bitwiseOr:
            case BitwiseXorView bitwiseXor:
            case BitwiseAndView bitwiseAnd:
            case BitShiftView bitShift:
                throw new NotImplementedException();

            case SumView sum:
            {
                var left = getExpressionRegister(builder, sum.Left);
                var right = getExpressionRegister(builder, sum.Right);
                var opcode = sum.Operator.Type switch
                {
                    TokenType.Plus => Opcode.Add,
                    TokenType.Minus => Opcode.Sub,
                    _ => throw new UnreachableException(),
                };

                builder.RegisterToRegister(opcode, resultRegister, left, right);
                break;
            }
            case TermView term:
            {
                var left = getExpressionRegister(builder, term.Left);
                var right = getExpressionRegister(builder, term.Right);
                var opcode = term.Operator.Type switch
                {
                    TokenType.Star => Opcode.Mul,
                    TokenType.Slash => Opcode.TDiv,
                    TokenType.Percent => Opcode.Mod,
                    TokenType.DoubleSlash => throw new NotImplementedException(),
                    TokenType.At => throw new NotImplementedException(),
                    _ => throw new UnreachableException(),
                };

                builder.RegisterToRegister(opcode, resultRegister, left, right);
                break;
            }
            case FactorView factor:
            case PowerView power:
            case AwaitPrimaryView awaitPrimary:
            case DotOperationPrimaryView dotPrimary:
            case SubscriptPrimaryView subscript:
            case CallWithGeneratorPrimaryView callGenerator:
                throw new NotImplementedException();

            case CallWithArgumentsPrimaryView callArgs:
            {
                var funcRegister = getExpressionRegister(builder, callArgs.Function);

                Register[] argRegistersArray = [];
                Span<Register> argRegisters = default;
                bool shouldReturnArray = false;
                try
                {
                    switch (callArgs.Arguments)
                    {
                        case null:
                            break;

                        case ArgumentsWithOnlyKeywordsView onlyKeywords:
                        {
                            throw new NotImplementedException();
                        }

                        case ArgumentsWithPositionalView positional:
                        {
                            if (positional.KeywordArgumentsPart != null)
                            {
                                throw new NotImplementedException();
                            }

                            int argsSize = positional.PositionalArgumentsPart.Length;
                            argRegistersArray = ArrayPool<Register>.Shared.Rent(argsSize);
                            argRegisters = argRegistersArray.AsSpan(0, argsSize);
                            shouldReturnArray = true;

                            for (int i = 0; i < argsSize; i++)
                            {
                                argRegisters[i] = positional.PositionalArgumentsPart[i] switch
                                {
                                    INamedExpressionView named => getExpressionRegister(builder, named),
                                    StarredExpressionView => throw new NotImplementedException(),
                                    _ => throw new UnreachableException(),
                                };
                            }

                            break;
                        }
                        default:
                            throw new UnreachableException();
                    }

                    // Place all call stuff in a row
                    int callId = builder.BeginNewCall();
                    int relativeRegisterAddress = 0;
                    int callSize = 2 + argRegisters.Length;
                    funcRegister = builder.Move(builder.AllocateRegister(callId, relativeRegisterAddress++, callSize), funcRegister);

                    resultRegister.CallId = callId;
                    resultRegister.CallRelativeAddress = relativeRegisterAddress++;
                    resultRegister.CallCount = callSize;
                    builder.LdConst(resultRegister, CodeBuilder.NoneConstantIndex);

                    for (int i = 0; i < argRegisters.Length; i++)
                    {
                        argRegisters[i] = builder.Move(builder.AllocateRegister(callId, relativeRegisterAddress++, callSize), argRegisters[i]);
                    }
                }
                finally
                {
                    if (shouldReturnArray)
                        ArrayPool<Register>.Shared.Return(argRegistersArray);
                }

                builder.Call(funcRegister, resultRegister, argRegisters.Length);
                break;
            }

            case AtomPrimaryView atom:
            {
                return atom.Atom switch
                {
                    NamedGroupExpressionView namedGroupExpression => getExpressionRegister(builder, namedGroupExpression.Value),

                    YieldGroupExpressionView yieldGroupExpression => throw new NotImplementedException(),

                    NameAtomView nameAtom => getVariable(builder, nameAtom.Value.RawString),

                    NumberAtomView number => builder.LdConst(resultRegister,
                        builder.AddConstant(NumberParser.GetNumberType(number.Value.RawString) switch
                        {
                            NumberType.Integer => (EpInteger)NumberParser.ParseInteger(number.Value.RawString),
                            NumberType.Float => (EpFloat)NumberParser.ParseFloat(number.Value.RawString),
                            // TODO: (EpComplex)NumberParser.ParseComplex(number.Value.RawString),
                            NumberType.Complex => throw new NotImplementedException(),
                            _ => throw new UnreachableException(),
                        })),

                    StringValueAtomView str => loadString(builder, resultRegister, str),

                    StringTemplateAtomView tStr => throw new NotImplementedException(),

                    NoneAtomView => builder.LdConst(resultRegister, CodeBuilder.NoneConstantIndex),
                    TrueAtomView => builder.LdConst(resultRegister, CodeBuilder.TrueConstantIndex),
                    FalseAtomView => builder.LdConst(resultRegister, CodeBuilder.FalseConstantIndex),
                    EllipsisAtomView => builder.LdConst(resultRegister, CodeBuilder.EllipsisConstantIndex),

                    TupleView tuple => throw new NotImplementedException(),
                    GeneratorExpressionView generatorExpr => throw new NotImplementedException(),

                    ListView list => throw new NotImplementedException(),
                    ListComprehensionView listComp => throw new NotImplementedException(),

                    DictView dict => throw new NotImplementedException(),
                    DictComprehensionView dictComp => throw new NotImplementedException(),

                    SetView set => throw new NotImplementedException(),
                    SetComprehensionView setComp => throw new NotImplementedException(),

                    _ => throw new UnreachableException()
                };
            }

            default:
                throw new UnreachableException();
        }

        return resultRegister;
    }

    private Register generateConjunction(CodeBuilder builder, ConjunctionView conj)
    {
        var shortCircuit = new Label();
        var resultRegister = builder.AllocateRegister();
        foreach (var inversion in conj.Inversions)
        {
            var expressionRegister = inversion switch
            {
                InversionView inv => generateInversion(builder, inv),
                IComparisonExpressionView comparisonExpression => generateComparison(builder, comparisonExpression),
                _ => throw new UnreachableException(),
            };
            builder.Move(resultRegister, expressionRegister);
            builder.BrFl(shortCircuit, resultRegister);
        }
        builder.PutLabel(shortCircuit);

        return resultRegister;
    }

    private Register generateInversion(CodeBuilder builder, InversionView inversion) => throw new NotImplementedException();

    private Register generateComparison(CodeBuilder builder, IComparisonExpressionView comparisonExpression)
    {
        switch (comparisonExpression)
        {
            case IBitwiseOrExpressionView bitwiseOr:
                return getExpressionRegister(builder, bitwiseOr);

            case ComparisonView comparison:
            {
                var shortCircuit = new Label();
                var resultRegister = builder.AllocateRegister();

                var leftRegister = getExpressionRegister(builder, comparison.First);

                foreach (var rhs in comparison.Rest)
                {
                    var rightRegister = getExpressionRegister(builder, rhs.Operand);
                    builder.RegisterToRegister(rhs.Operation switch
                    {
                        CompareOperation.Equals => Opcode.Eq,
                        CompareOperation.NotEquals => Opcode.NEq,
                        CompareOperation.LessThan => Opcode.LsTh,
                        CompareOperation.LessThanEquals => throw new NotImplementedException(),
                        CompareOperation.GreaterThan => Opcode.GrTh,
                        CompareOperation.GreaterThanEquals => throw new NotImplementedException(),
                        CompareOperation.In => throw new NotImplementedException(),
                        CompareOperation.NotIn => throw new NotImplementedException(),
                        CompareOperation.Is => throw new NotImplementedException(),
                        CompareOperation.IsNot => throw new NotImplementedException(),

                        _ => throw new UnreachableException(),
                    }, resultRegister, leftRegister, rightRegister);
                    builder.BrFl(shortCircuit, resultRegister);

                    leftRegister = rightRegister;
                }

                builder.PutLabel(shortCircuit);

                return resultRegister;
            }

            default:
                throw new UnreachableException();
        }
    }

    private static Register loadString(CodeBuilder builder, Register resultRegister, StringValueAtomView str)
    {
        if (str.Parts.Any(p => p is FStringView))
        {
            throw new NotImplementedException();
        }

        string strValue;
        switch (str.Parts.Count)
        {
            case 1:
                strValue = ((StringConstantView)str.Parts[0]).Value.RawString;
                strValue = StringParser.ParseQuoted(strValue);
                break;
            case 2:
                // TODO: StringParser.ParseQuotedMany(str.Parts)
                string part0 = ((StringConstantView)str.Parts[0]).Value.RawString;
                part0 = StringParser.ParseQuoted(part0);
                string part1 = ((StringConstantView)str.Parts[1]).Value.RawString;
                part1 = StringParser.ParseQuoted(part1);
                strValue = part0 + part1;
                break;
            default:
                var sb = new StringBuilder();

                foreach (var part in str.Parts)
                {
                    string partValue = ((StringConstantView)part).Value.RawString;
                    sb.Append(StringParser.ParseQuoted(partValue));
                }

                strValue = sb.ToString();
                break;
        }

        return builder.LdConst(resultRegister, builder.AddConstant(new EpString(strValue)));
    }

    private void beginLoop(CodeBuilder builder, IntermediateLoop newLoop)
    {
        if (loops.Count == 0)
        {
            builder.BeginLoopLifetime();
        }

        loops.Push(newLoop);
    }

    private void endLoop(CodeBuilder builder)
    {
        loops.Pop();

        if (loops.Count == 0)
        {
            builder.EndLoopLifetime();
        }
    }

    private void storeVariable(CodeBuilder builder, Variable variable, Register source)
    {
        if (variable.Kind == VariableKind.Global)
        {
            builder.StVar(source, builder.AddVariable(variable.Name));
        }
        else if (variable.Kind == VariableKind.Cell)
        {
            throw new NotImplementedException();
            // builder.StCell(variableValue, variable.CellNumber);
        }
        else if (inLoop)
        {
            Debug.Assert(variable.Register != null);

            builder.AddToLoopLifetime(variable.Register);
        }
    }

    private Register getVariable(CodeBuilder builder, string variableName)
    {
        var resultRegister = builder.AllocateRegister();

        if (variables.TryGetValue(variableName, out var variable))
        {
            if (variable.Kind == VariableKind.Cell)
            {
                throw new NotImplementedException();
                // return builder.LdCell(resultRegister, variable.CellNumber);
            }
            else if (variable.Kind == VariableKind.Local && inLoop)
            {
                Debug.Assert(variable.Register != null);

                builder.AddToLoopLifetime(variable.Register);
                return variable.Register;
            }
        }

        return builder.LdVar(resultRegister, builder.AddVariable(variableName));
    }
}
