using System.Diagnostics;
using System.Text;
using Epoxid.Runtime.Objects;
using Epoxid.SyntaxAnalysis;
using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.CodeGen;

internal static class BlockGenerator
{
    public static void Generate(CodeBuilder builder, FileView module)
    {
        generateStatements(builder, module.Statements);

        if (builder.RequiresReturnNone)
        {
            builder.LdConst(CodeBuilder.NoneConstantIndex);
            builder.Ret();
        }
    }

    private static void generateStatements(CodeBuilder builder, IEnumerable<IStatementView> statements)
    {
        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case SingleSimpleStatementView single:
                    simpleStatement(builder, single.Value);
                    break;

                case SeparatedSimpleStatementsView separated:
                    foreach (var simple in separated.Values)
                    {
                        simpleStatement(builder, simple);
                    }
                    break;

                case FunctionDefView function:
                    throw new NotImplementedException();

                case ClassDefView classDef:
                    throw new NotImplementedException();

                case IfStatementView ifStmt:
                {
                    var elseLabel = builder.CreateLabel();
                    var endLabel = builder.CreateLabel();

                    namedExpression(builder, ifStmt.Condition);
                    builder.BrPopFl(elseLabel);

                    generateBlock(builder, ifStmt.Block);
                    builder.BrcForw(endLabel);

                    foreach (var elif in ifStmt.Elifs)
                    {
                        builder.PutLabel(elseLabel);
                        elseLabel = builder.CreateLabel();

                        namedExpression(builder, elif.Condition);
                        builder.BrPopFl(elseLabel);

                        generateBlock(builder, elif.Block);
                        builder.BrcForw(endLabel);
                    }

                    builder.PutLabel(elseLabel);
                    if (ifStmt.Else != null)
                    {
                        generateBlock(builder, ifStmt.Else.Block);
                    }
                    builder.PutLabel(endLabel);

                    break;
                }
                case WhileStatementView whileStmt:
                {
                    var headLabel = builder.CreateLabel();
                    var elseLabel = builder.CreateLabel();
                    var endLabel = builder.CreateLabel();

                    builder.BeginLoop(new Loop(headLabel, endLabel));

                    builder.PutLabel(headLabel);
                    namedExpression(builder, whileStmt.Condition);
                    builder.BrPopFl(elseLabel);

                    generateBlock(builder, whileStmt.Block);
                    builder.BrcBack(headLabel);

                    builder.PutLabel(elseLabel);
                    if (whileStmt.Else != null)
                    {
                        generateBlock(builder, whileStmt.Else.Block);
                    }
                    builder.PutLabel(endLabel);

                    builder.EndLoop();

                    break;
                }
                case ForStatementView forStmt:
                    throw new NotImplementedException();

                case TryStatementView tryStmt:
                    throw new NotImplementedException();

                case WithStatementView withStmt:
                    throw new NotImplementedException();

                default:
                    throw new UnreachableException();
            }
        }
    }

    private static void generateBlock(CodeBuilder builder, BlockView block) => generateStatements(builder, block.GetStatements());

    private static void simpleStatement(CodeBuilder builder, ISimpleStatementView stmt)
    {
        switch (stmt)
        {
            // TODO: remove simple assignment
            case SimpleAssignmentView simple:
            {
                string name = simple.Target.RawString;
                annotatedRhs(builder, simple.Rhs.Value);
                storeVariable(builder, name);
                break;
            }
            case AugmentedAssignmentView augAssign:
                augmentedAssignment(builder, augAssign.Target, augAssign.Rhs, augAssign.Operation);
                break;

            case PassStatementView:
                break;

            case ReturnStatementView ret:
            case BreakStatementView brk:
            case ContinueStatementView cont:
                throw new NotImplementedException();

            case IStarExpressionView starred:
                starExpression(builder, starred);
                break;

            case StarExpressionsView starredMany:
                starExpression(builder, starredMany);
                break;

            case AssertStatementView:
            case IImportStatementView:
            case RaiseStatementView:
            case NonlocalStatementView:
            case TypeAliasView:
            case DeleteStatementView:
            case YieldStatementView:
            case GlobalStatementView:
                throw new NotImplementedException();

            case AnnotatedSubscriptAttributeAssignmentView:
            case AnnotatedAssignmentView:
            case AnnotatedParenthesizedAssignmentView:
            case CascadeAssignmentView:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
        builder.ClearStack(stmt);
    }

    private static void augmentedAssignment(CodeBuilder builder, SingleTargetView target, IAnnotatedRhsView rhs, AugmentedAssignment operation)
    {
        switch (target)
        {
            case NameSingleTargetView nameTarget:
            {
                string name = nameTarget.Value.RawString;
                loadVariable(builder, name);
                annotatedRhs(builder, rhs);
                augmentedAssignmentOperation(builder, operation);
                storeVariable(builder, name);
                break;
            }
            case GroupedTargetView grouped:
                augmentedAssignment(builder, grouped.Target, rhs, operation);
                break;

            case CompositeSingleTargetView composite:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
    }

    private static void augmentedAssignmentOperation(CodeBuilder builder, AugmentedAssignment operation)
    {
        switch (operation)
        {
            case AugmentedAssignment.Add:
                builder.InAdd();
                break;
            case AugmentedAssignment.Subtract:
                builder.InSub();
                break;
            case AugmentedAssignment.Multiply:
                builder.InMul();
                break;
            case AugmentedAssignment.MatrixMultiply:
                throw new NotImplementedException();
            case AugmentedAssignment.TrueDivide:
                builder.InTDiv();
                break;
            case AugmentedAssignment.FloorDivide:
                throw new NotImplementedException();
            case AugmentedAssignment.Module:
                builder.InMod();
                break;
            case AugmentedAssignment.Power:
                throw new NotImplementedException();
            case AugmentedAssignment.LeftShift:
                throw new NotImplementedException();
            case AugmentedAssignment.RightShift:
                throw new NotImplementedException();
            case AugmentedAssignment.BitwiseAnd:
                throw new NotImplementedException();
            case AugmentedAssignment.BitwiseXor:
                throw new NotImplementedException();
            case AugmentedAssignment.BitwiseOr:
                throw new NotImplementedException();
            default:
                throw new UnreachableException($"Invalid AugmentedAssignment value: {operation}");
        }
    }

    private static void storeVariable(CodeBuilder builder, string name)
    {
        var variable = builder.SymbolTable.Symbols[name];
        switch (variable.Kind)
        {
            case VariableKind.Local:
            {
                int index = builder.AddVariable(variable.Name);
                builder.StLocal(index);
                break;
            }
            case VariableKind.Cell:
            {
                int index = builder.AddVariable(variable.Name);
                builder.StCell(index);
                break;
            }
            case VariableKind.GlobalExplicit:
            {
                int index = builder.AddVariable(variable.Name);
                builder.StVar(index);
                break;
            }
            case VariableKind.GlobalImplicit:
            case VariableKind.Free:
                throw new UnreachableException($"Invalid VariableKind value in this context: {variable.Kind}");

            default:
                throw new UnreachableException($"Invalid VariableKind value: {variable.Kind}");
        }
    }

    private static void annotatedRhs(CodeBuilder builder, IAnnotatedRhsView rhs)
    {
        switch (rhs)
        {
            case IStarExpressionView star:
                starExpression(builder, star);
                break;

            case StarExpressionsView stars:
                starExpression(builder, stars);
                break;

            case YieldExpressionView yield:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
    }

    private static void starExpression(CodeBuilder builder, StarExpressionsView starred) => throw new NotImplementedException();

    private static void starExpression(CodeBuilder builder, IStarExpressionView starred)
    {
        switch (starred)
        {
            case IExpressionView expr:
                namedExpression(builder, expr);
                break;

            case StarBitwiseOrExpressionView starredBitwiseOr:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
    }

    private static void namedExpression(CodeBuilder builder, INamedExpressionView expr)
    {
        switch (expr)
        {
            case IfExpressionView ifExpr:
            {
                var elseLabel = builder.CreateLabel();
                var endLabel = builder.CreateLabel();

                namedExpression(builder, ifExpr.Condition);
                builder.BrPopFl(elseLabel);
                namedExpression(builder, ifExpr.Then);
                builder.BrcForw(endLabel);

                builder.PutLabel(elseLabel);
                namedExpression(builder, ifExpr.Else);
                builder.PutLabel(endLabel);

                break;
            }
            case AssignmentExpressionView assignmentExpression:
                throw new NotImplementedException();

            case DisjunctionView disj:
                disjunction(builder, disj);
                break;

            case BitwiseOrView bOr:
                throw new NotImplementedException();

            case BitwiseXorView bXor:
                throw new NotImplementedException();

            case BitwiseAndView bAnd:
                throw new NotImplementedException();

            case BitShiftView bShift:
                throw new NotImplementedException();

            case SumView sum:
            {
                namedExpression(builder, sum.Left);
                namedExpression(builder, sum.Right);
                switch (sum.Operator.Type)
                {
                    case TokenType.Plus:
                        builder.Add();
                        break;
                    case TokenType.Minus:
                        builder.Sub();
                        break;
                    default:
                        throw new UnreachableException();
                }
                break;
            }
            case TermView term:
            {
                namedExpression(builder, term.Left);
                namedExpression(builder, term.Right);
                switch (term.Operator.Type)
                {
                    case TokenType.Star:
                        builder.Mul();
                        break;
                    case TokenType.Slash:
                        builder.TDiv();
                        break;
                    case TokenType.Percent:
                        builder.Mod();
                        break;
                    case TokenType.DoubleSlash:
                        throw new NotImplementedException();
                    case TokenType.At:
                        throw new NotImplementedException();
                    default:
                        throw new UnreachableException();
                }
                break;
            }
            case FactorView factor:
                throw new NotImplementedException();
            case PowerView power:
                throw new NotImplementedException();

            case AtomPrimaryView atomPrimary:
                atom(builder, atomPrimary.Atom);
                break;

            case SubscriptPrimaryView subscript:
                throw new NotImplementedException();

            case DotOperationPrimaryView attr:
            {
                int attrNameIndex = builder.AddVariable(attr.Right.RawString);

                namedExpression(builder, attr.Left);
                builder.GAttr(attrNameIndex);
                break;
            }
            case CallWithArgumentsPrimaryView call:
                callPrimary(builder, call);
                break;

            case CallWithGeneratorPrimaryView generatorCall:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
    }

    private static void disjunction(CodeBuilder builder, DisjunctionView disj)
    {
        if (disj.Conjunctions.Length == 1)
        {
            conjunction(builder, disj.Conjunctions[0]);
            return;
        }

        var shortWay = builder.CreateLabel();
        bool insertBranch = false;
        foreach (var conj in disj.Conjunctions)
        {
            if (insertBranch)
            {
                builder.BrPeekTr(shortWay);
                insertBranch = true;
            }
            conjunction(builder, conj);
        }
        builder.PutLabel(shortWay);
    }

    private static void conjunction(CodeBuilder builder, ConjunctionView conj)
    {
        if (conj.Inversions.Length == 1)
        {
            inversion(builder, conj.Inversions[0]);
            return;
        }

        var shortWay = builder.CreateLabel();
        bool insertBranch = false;
        foreach (var inv in conj.Inversions)
        {
            if (insertBranch)
            {
                builder.BrPeekFl(shortWay);
                insertBranch = true;
            }
            inversion(builder, inv);
        }
        builder.PutLabel(shortWay);
    }

    private static void inversion(CodeBuilder builder, IInversionExpressionView inversion)
    {
        switch (inversion)
        {
            case InversionView inv:
                throw new NotImplementedException();

            case ComparisonView comparison:
            {
                namedExpression(builder, comparison.First);

                foreach (var compare in comparison.Rest)
                {
                    namedExpression(builder, compare.Operand);

                    switch (compare.Operation)
                    {
                        case CompareOperation.Equals:
                            builder.Eq();
                            break;

                        case CompareOperation.NotEquals:
                            builder.NEq();
                            break;

                        case CompareOperation.LessThan:
                            builder.LsTh();
                            break;

                        case CompareOperation.LessThanEquals:
                            throw new NotImplementedException();

                        case CompareOperation.GreaterThan:
                            builder.GrTh();
                            break;

                        case CompareOperation.GreaterThanEquals:
                            throw new NotImplementedException();

                        case CompareOperation.In:
                            throw new NotImplementedException();

                        case CompareOperation.NotIn:
                            throw new NotImplementedException();

                        case CompareOperation.Is:
                            throw new NotImplementedException();

                        case CompareOperation.IsNot:
                            throw new NotImplementedException();

                        default:
                            throw new UnreachableException();
                    }
                }
                break;
            }
            default:
                throw new UnreachableException();
        }
    }

    private static void callPrimary(CodeBuilder builder, CallWithArgumentsPrimaryView call)
    {
        namedExpression(builder, call.Function);

        int argCount = 0;
        switch (call.Arguments)
        {
            case ArgumentsWithPositionalView withPositional:
            {
                foreach (var positional in withPositional.PositionalArgumentsPart)
                {
                    if (positional is INamedExpressionView named)
                    {
                        namedExpression(builder, named);
                    }
                    else if (positional is StarredExpressionView)
                    {
                        throw new NotImplementedException();
                    }
                    else
                        throw new UnreachableException();
                }
                argCount = withPositional.PositionalArgumentsPart.Length;
                break;
            }
            case ArgumentsWithOnlyKeywordsView:
                throw new NotImplementedException();
        }

        builder.Call(argCount);
    }

    private static void atom(CodeBuilder builder, IAtomView atom)
    {
        switch (atom)
        {
            case SetComprehensionView setComp:
                throw new NotImplementedException();

            case SetView set:
                throw new NotImplementedException();

            case GeneratorExpressionView generator:
                throw new NotImplementedException();

            case TupleView tuple:
                throw new NotImplementedException();

            case DictComprehensionView dictComp:
                throw new NotImplementedException();

            case DictView dict:
                throw new NotImplementedException();

            case ListComprehensionView listComp:
                throw new NotImplementedException();

            case ListView list:
                throw new NotImplementedException();

            case TrueAtomView:
                builder.LdConst(CodeBuilder.TrueConstantIndex);
                break;

            case FalseAtomView:
                builder.LdConst(CodeBuilder.FalseConstantIndex);
                break;

            case NoneAtomView:
                builder.LdConst(CodeBuilder.NoneConstantIndex);
                break;

            case EllipsisAtomView:
                builder.LdConst(CodeBuilder.EllipsisConstantIndex);
                break;

            case NumberAtomView numberAtom:
            {
                string number = numberAtom.Value.RawString;
                int constIndex = NumberParser.GetNumberType(number) switch
                {
                    NumberType.Integer => builder.AddConstant(new EpInteger((int)NumberParser.ParseInteger(number))),

                    NumberType.Float => builder.AddConstant(new EpFloat(NumberParser.ParseFloat(number))),

                    NumberType.Complex => throw new NotImplementedException(),

                    _ => throw new UnreachableException(),
                };
                builder.LdConst(constIndex);
                break;
            }

            case NameAtomView nameAtom:
            {
                loadVariable(builder, nameAtom.Value.RawString);
                break;
            }

            case StringValueAtomView str:
                stringAtom(builder, str);
                break;

            case StringTemplateAtomView tStr:
                stringTemplateAtom(builder, tStr);
                break;

            case NamedGroupExpressionView namedGroup:
                namedExpression(builder, namedGroup.Value);
                break;

            case YieldGroupExpressionView yieldGroup:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
    }

    private static void loadVariable(CodeBuilder builder, string variableName)
    {
        if (!builder.SymbolTable.Symbols.TryGetValue(variableName, out var variable))
        {
            throw new InvalidOperationException($"Symbol table doesn't contain record on the variable '{variableName}'");
        }
        switch (variable.Kind)
        {
            case VariableKind.Cell:
            case VariableKind.Free:
                throw new NotImplementedException();

            case VariableKind.Local:
                // TODO: also arguments
                throw new NotImplementedException("Indexes for the variables that were saved in the function environment");

            case VariableKind.GlobalExplicit:
            case VariableKind.GlobalImplicit:
            {
                int index = builder.AddVariable(variable.Name);
                builder.LdVar(index);
                break;
            }
        }
    }

    private static void stringTemplateAtom(CodeBuilder builder, StringTemplateAtomView tStr) => throw new NotImplementedException();

    private static void stringAtom(CodeBuilder builder, StringValueAtomView str)
    {
        // TODO: more optimized way to concatenate strings.
        // TODO: f-strings
        var sb = new StringBuilder();
        foreach (var part in str.Parts)
        {
            switch (part)
            {
                case FStringView fStr:
                    throw new NotImplementedException();

                case StringConstantView strConst:
                {
                    string raw = strConst.Value.RawString;
                    if (StringParser.HasPrefix(raw))
                    {
                        throw new NotImplementedException();
                    }

                    string value = StringParser.ParseQuoted(raw);
                    sb.Append(value);
                    break;
                }
                default:
                    throw new UnreachableException();
            }
        }

        string result = sb.ToString();
        int index = builder.AddConstant((EpString)result);
        builder.LdConst(index);
    }
}
