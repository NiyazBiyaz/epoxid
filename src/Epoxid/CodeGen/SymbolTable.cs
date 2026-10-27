using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Epoxid.SyntaxAnalysis;

namespace Epoxid.CodeGen;

internal class SymbolTable
{
    public string Owner { get; }

    public Dictionary<string, Variable> Symbols { get; } = [];

    public List<SymbolTable> ChildSymbolTables { get; } = [];

    private SymbolTable? parent { get; init; }

    private DeclarationScopeKind scopeKind { get; init; }

    private bool isModule => parent == null;

    private readonly List<(BlockView block, List<Variable> parameters, string qualifiedName, bool isFunction)> deferredChildren = [];

    private SymbolTable(string owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// Creates new <see cref="SymbolTable"/> object from <paramref name="module"/> scanning all its content
    /// </summary>
    /// <param name="module">Epoxid file view</param>
    public static SymbolTable Create(FileView module, string moduleName)
    {
        var ret = new SymbolTable(moduleName)
        {
            scopeKind = DeclarationScopeKind.Global,
        };
        ret.fillSymbols(module.Statements, []);
        return ret;
    }

    private void fillSymbols(IEnumerable<IStatementView> statements, List<Variable> parameters)
    {
        foreach (var param in parameters)
        {
            Symbols[param.Name] = param;
        }

        fillWithStatements(statements);

        foreach (var (block, childParameters, qualName, isFunction) in deferredChildren)
        {
            addChildTable(block, childParameters, qualName, isFunction);
        }

        if (isModule)
        {
            foreach (var symbol in Symbols.Values)
            {
                if (!symbol.Usage.HasFlag(VariableUsages.Used) || symbol.Usage.HasFlag(VariableUsages.DefinedLocal))
                    symbol.Kind = VariableKind.GlobalExplicit;
            }
        }

        foreach (var (i, symbol) in Symbols.Values.Index())
        {
            symbol.LocalIndex = i;
        }
    }

    private void fillWithStatements(IEnumerable<IStatementView> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case SeparatedSimpleStatementsView separated:
                    foreach (var stmt in separated.Values)
                    {
                        fillWithSimpleStmt(stmt);
                    }
                    break;

                case SingleSimpleStatementView single:
                    fillWithSimpleStmt(single.Value);
                    break;

                case FunctionDefView func:
                    variableAssigned(func.Name.RawString);
                    foreach (var decorator in func.Decorators)
                    {
                        fillWithExpression(decorator.Value);
                    }
                    // Because we need to know when local variable should became a closed variable, we first need to
                    // know all variables. So we need to end this block scope first and then we can analyze another one
                    defer(func);
                    break;

                case ClassDefView cls:
                    variableAssigned(cls.Name.RawString);
                    if (cls.Arguments != null && cls.Arguments.Value != null)
                        fillWithArguments(cls.Arguments.Value);

                    foreach (var decorator in cls.Decorators)
                    {
                        fillWithExpression(decorator.Value);
                    }
                    // See above
                    defer(cls);
                    break;

                case WhileStatementView whileStmt:
                    fillWithExpression(whileStmt.Condition);
                    fillWithBlock(whileStmt.Block);
                    if (whileStmt.Else != null)
                        fillWithBlock(whileStmt.Else.Block);
                    break;

                case ForStatementView forStmt:
                {
                    switch (forStmt.Targets)
                    {
                        case IAssignmentTargetView target:
                            fillWithAssignmentTarget(target);
                            break;

                        case AssignmentTargetsView targets:
                            fillWithAssignmentTarget(targets);
                            break;

                        default:
                            throw new UnreachableException();
                    }
                    switch (forStmt.Expression)
                    {
                        case IStarExpressionView starExpression:
                            fillWithStarExpression(starExpression);
                            break;

                        case StarExpressionsView starExpressions:
                            fillWithStarExpression(starExpressions);
                            break;

                        default:
                            throw new UnreachableException();
                    }
                    fillWithBlock(forStmt.Block);
                    if (forStmt.Else != null)
                        fillWithBlock(forStmt.Else.Block);

                    break;
                }
                case IfStatementView ifStmt:
                    fillWithExpression(ifStmt.Condition);
                    fillWithBlock(ifStmt.Block);
                    foreach (var elif in ifStmt.Elifs)
                    {
                        fillWithExpression(elif.Condition);
                        fillWithBlock(elif.Block);
                    }
                    if (ifStmt.Else != null)
                        fillWithBlock(ifStmt.Else.Block);
                    break;

                case TryStatementView:
                case WithStatementView:
                    throw new NotImplementedException();

                default:
                    throw new UnreachableException();
            }
        }
    }

    private void fillWithAssignmentTarget(AssignmentTargetsView targets)
    {
        foreach (var target in targets.Targets)
        {
            fillWithAssignmentTarget(target);
        }
    }

    private void fillWithAssignmentTarget(IAssignmentTargetView assignmentTarget)
    {
        switch (assignmentTarget)
        {
            case NameTargetAtomView nameAtom:
                variableAssigned(nameAtom.Value.RawString);
                break;

            case GroupedTargetAtomView groupAtom:
                fillWithAssignmentTarget(groupAtom.Target);
                break;

            case ListTargetAtomView listAtom:
                if (listAtom.Items != null)
                    fillWithAssignmentTarget(listAtom.Items);
                break;

            case TupleTargetAtomView tupleAtom:
                if (tupleAtom.Items != null)
                    fillWithAssignmentTarget(tupleAtom.Items);
                break;

            case SubscriptTargetUnitView subscript:
                fillWithTargetPrimary(subscript.Left);
                fillWithSlices(subscript.Right);
                break;

            // In attribute only left part can be variable
            case DottedTargetUnitView dotted:
                fillWithTargetPrimary(dotted.Left);
                break;

            case StarredAssignmentTargetView starred:
                fillWithAssignmentTarget(starred.Target);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithTargetPrimary(TargetPrimaryView primary)
    {
        switch (primary)
        {
            // In attribute only left part can be variable
            case AttributeTargetView attr:
                fillWithTargetPrimary(attr.Primary);
                break;

            case CallTargetView call:
                fillWithTargetPrimary(call.Primary);
                if (call.Arguments != null)
                    fillWithArguments(call.Arguments);
                break;

            case GeneratorTargetView generator:
                fillWithTargetPrimary(generator.Primary);
                fillWithGeneratorExpression(generator.Generator);
                break;

            case SubscriptTargetView subscript:
                fillWithTargetPrimary(subscript.Primary);
                fillWithSlices(subscript.Subscript);
                break;

            case AtomTargetView atom:
                fillWithAtom(atom.Atom);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithSlices(SlicesView subscript) => throw new NotImplementedException();

    private void fillWithGeneratorExpression(GeneratorExpressionView generator) => throw new NotImplementedException();

    private void fillWithArguments(ArgumentsView arguments)
    {
        switch (arguments)
        {
            case ArgumentsWithOnlyKeywordsView onlyKeywords:
                fillWithKwargs(onlyKeywords.Value);
                break;

            case ArgumentsWithPositionalView withPositional:
                foreach (var positional in withPositional.PositionalArgumentsPart)
                {
                    switch (positional)
                    {
                        case INamedExpressionView named:
                            fillWithExpression(named);
                            break;

                        case StarredExpressionView starred:
                            fillWithExpression(starred.Expression);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                if (withPositional.KeywordArgumentsPart != null)
                    fillWithKwargs(withPositional.KeywordArgumentsPart.Value);

                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithKwargs(KwargsView kwargs)
    {
        switch (kwargs)
        {
            case BothStarredKwargsView both:
                foreach (var oneStarred in both.OneStarred)
                {
                    fillWithOneStarredKwarg(oneStarred);
                }
                foreach (var doubleStarred in both.DoubleStarred)
                {
                    fillWithDoubleStarredKwarg(doubleStarred);
                }
                break;

            case OneStarredKwargsView oneStarredKwargs:
                foreach (var oneStarred in oneStarredKwargs.Items)
                {
                    fillWithOneStarredKwarg(oneStarred);
                }
                break;

            case DoubleStarredKwargsView doubleStarredKwargs:
                foreach (var doubleStarred in doubleStarredKwargs.Items)
                {
                    fillWithDoubleStarredKwarg(doubleStarred);
                }
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithOneStarredKwarg(IKwargOrStarredView oneStarred)
    {
        switch (oneStarred)
        {
            case IStarExpressionView starred:
                fillWithStarExpression(starred);
                break;

            case KwargView kwarg:
                fillWithExpression(kwarg.Value);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithDoubleStarredKwarg(IKwargOrDoubleStarredView doubleStarred)
    {
        switch (doubleStarred)
        {
            case DoubleStarredExpressionView starred:
                fillWithExpression(starred.Expression);
                break;

            case KwargView kwarg:
                fillWithExpression(kwarg.Value);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithSimpleStmt(ISimpleStatementView stmt)
    {
        switch (stmt)
        {
            case ContinueStatementView:
            case BreakStatementView:
            case DeleteStatementView:
            case PassStatementView:
                break;

            case TypeAliasView:
                throw new NotImplementedException();

            case AssertStatementView assert:
            case RaiseStatementView raise:
            case YieldStatementView yield:
            case IImportStatementView import:
            case NonlocalStatementView nonlocal:
            case GlobalStatementView global:
                throw new NotImplementedException();

            case AssignmentView assignment:
                fillWithAssignment(assignment);
                break;

            case ReturnStatementView ret:
                switch (ret.Expression)
                {
                    case IStarExpressionView starExpression:
                        fillWithStarExpression(starExpression);
                        break;

                    case StarExpressionsView starExpressions:
                        fillWithStarExpression(starExpressions);
                        break;

                    default:
                        throw new UnreachableException();
                }
                break;

            case IStarExpressionVariantView expr:
                switch (expr)
                {
                    case IStarExpressionView starred:
                        fillWithStarExpression(starred);
                        break;

                    case StarExpressionsView manyStarred:
                        fillWithStarExpression(manyStarred);
                        break;
                    default:
                        throw new UnreachableException();
                }
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithAssignment(AssignmentView assignment)
    {
        switch (assignment)
        {
            case CascadeAssignmentView cascade:
            {
                foreach (var target in cascade.Targets.Select(t => t.Value))
                {
                    switch (target)
                    {
                        case IAssignmentTargetView t:
                            fillWithAssignmentTarget(t);
                            break;
                        case AssignmentTargetsView targets:
                            fillWithAssignmentTarget(targets);
                            break;
                        default:
                            throw new UnreachableException();
                    }
                }

                fillWithAnnotatedRhs(cascade.Rhs);
                break;
            }
            case AugmentedAssignmentView augmented:
                fillWithSingleAssignmentTarget(augmented.Target);
                fillWithAnnotatedRhs(augmented.Rhs);
                break;

            case AnnotatedParenthesizedAssignmentView:
            case AnnotatedCompositeTargetAssignmentView:
            case AnnotatedAssignmentView:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithSingleAssignmentTarget(ISingleTargetView target)
    {
        switch (target)
        {
            case NameSingleTargetAtomView name:
                variableAssigned(name.Name.RawString);
                break;

            case GroupedTargetView grouped:
                fillWithSingleAssignmentTarget(grouped.Target);
                break;

            case DottedTargetUnitView dotted:
                fillWithTargetPrimary(dotted.Left);
                break;

            case SubscriptTargetUnitView subscript:
                fillWithTargetPrimary(subscript.Left);
                fillWithSlices(subscript.Right);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithStarExpression(IStarExpressionView starred)
    {
        switch (starred)
        {
            case IExpressionView expr:
                fillWithExpression(expr);
                break;

            case StarBitwiseOrExpressionView starBitwiseOr:
                fillWithExpression(starBitwiseOr.Value);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithStarExpression(StarExpressionsView starreds)
    {
        foreach (var starred in starreds.Values)
        {
            fillWithStarExpression(starred);
        }
    }

    private void fillWithStarNamedExpression(IStarNamedExpressionView starNamed)
    {
        switch (starNamed)
        {
            case INamedExpressionView expr:
                fillWithExpression(expr);
                break;

            case StarBitwiseOrExpressionView starBitwiseOr:
                fillWithStarExpression(starBitwiseOr);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithStarNamedExpression(StarNamedExpressionsView starNameds)
    {
        foreach (var starNamed in starNameds.Items)
            fillWithStarNamedExpression(starNamed);
    }

    private void fillWithExpression(INamedExpressionView expression)
    {
        switch (expression)
        {
            case DisjunctionView disjunction:
                fillWithDisjunction(disjunction);
                break;

            case BitwiseOrView bitwiseOr:
                fillWithExpression(bitwiseOr.Left);
                fillWithExpression(bitwiseOr.Right);
                break;

            case BitwiseXorView bitwiseXor:
                fillWithExpression(bitwiseXor.Left);
                fillWithExpression(bitwiseXor.Right);
                break;

            case BitwiseAndView bitwiseAnd:
                fillWithExpression(bitwiseAnd.Left);
                fillWithExpression(bitwiseAnd.Right);
                break;

            case BitShiftView bitShift:
                fillWithExpression(bitShift.Left);
                fillWithExpression(bitShift.Right);
                break;

            case SumView sum:
                fillWithExpression(sum.Left);
                fillWithExpression(sum.Right);
                break;

            case TermView term:
                fillWithExpression(term.Left);
                fillWithExpression(term.Right);
                break;

            case FactorView factor:
                fillWithExpression(factor.Value);
                break;

            case PowerView power:
                fillWithExpression(power.Left);
                fillWithExpression(power.Right);
                break;

            case PrimaryView primary:
                fillWithPrimary(primary);
                break;

            case IfExpressionView ifExpression:
                fillWithExpression(ifExpression.Condition);
                fillWithExpression(ifExpression.Then);
                fillWithExpression(ifExpression.Else);
                break;

            case AssignmentExpressionView assignment:
            {
                var name = assignment.Target.RawString;
                variableAssigned(name);
                fillWithExpression(assignment.Value);
                break;
            }

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithPrimary(PrimaryView primary)
    {
        switch (primary)
        {
            case CallWithGeneratorPrimaryView:
                throw new NotImplementedException();

            case SubscriptPrimaryView subscript:
                fillWithPrimary(subscript.Left);
                fillWithSlices(subscript.Right);
                break;

            case DotOperationPrimaryView dotted:
                fillWithPrimary(dotted.Left);
                break;

            case CallWithArgumentsPrimaryView call:
                fillWithPrimary(call.Function);
                if (call.Arguments != null)
                    fillWithArguments(call.Arguments);
                break;

            case AtomPrimaryView atom:
                fillWithAtom(atom.Atom);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithAtom(IAtomView atom)
    {
        switch (atom)
        {
            case NumberAtomView:
            case NoneAtomView:
            case FalseAtomView:
            case TrueAtomView:
            case EllipsisAtomView:
                break;

            case StringValueAtomView str:
            {
                foreach (var part in str.Parts)
                {
                    if (part is not FStringView fStr)
                        continue;

                    foreach (var v in fStr.Values.OfType<FStringValueReplacementView>())
                    {
                        fillWithFStringReplacement(v.Value);
                    }
                }

                break;
            }
            case StringTemplateAtomView tStr:
            {
                foreach (var part in tStr.Parts)
                {
                    foreach (var v in part.Values.OfType<TStringValueReplacementView>())
                    {
                        fillWithTStringReplacement(v.Value);
                    }
                }

                break;
            }
            case TupleView tuple:
            {
                fillWithStarNamedExpression(tuple.First);
                if (tuple.Rest != null)
                    fillWithStarNamedExpression(tuple.Rest);

                break;
            }
            case NameAtomView nameAtom:
            {
                string name = nameAtom.Value.RawString;
                variableUsed(name);
                break;
            }
            case DictView:
            case GroupView:
            case ListView:
            case SetView:
            case GeneratorExpressionView:
            case SetComprehensionView:
            case DictComprehensionView:
            case ListComprehensionView:
                throw new NotImplementedException();

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithFStringReplacement(FStringReplacementFieldView fStringReplacement)
    {
        fillWithAnnotatedRhs(fStringReplacement.Value);

        foreach (var spec in fStringReplacement.FormatSpec?.Specs ?? [])
        {
            if (spec is FStringFormatReplacementView replacement)
            {
                fillWithFStringReplacement(replacement.Value);
            }
        }
    }

    private void fillWithTStringReplacement(TStringReplacementFieldView tStringReplacement)
    {
        fillWithAnnotatedRhs(tStringReplacement.Value);

        foreach (var spec in tStringReplacement.FormatSpec?.Specs ?? [])
        {
            if (spec is TStringFormatReplacementView replacement)
            {
                fillWithTStringReplacement(replacement.Value);
            }
        }
    }

    private void fillWithAnnotatedRhs(IAnnotatedRhsView rhs)
    {
        switch (rhs)
        {
            case YieldFromExpressionView yieldExpr:
                fillWithExpression(yieldExpr.Expression);
                break;

            case YieldStarExpressionView yieldStar:
                if (yieldStar.Expression != null)
                    fillWithStarExpression(yieldStar.Expression);
                break;

            case IStarExpressionView starExpression:
                fillWithStarExpression(starExpression);
                break;

            case StarExpressionsView starExpressions:
                fillWithStarExpression(starExpressions);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithDisjunction(DisjunctionView disjunction)
    {
        foreach (var conjunction in disjunction.Conjunctions)
        {
            foreach (var inversion in conjunction.Inversions)
            {
                fillWithInversion(inversion);
            }
        }
    }

    private void fillWithInversion(IInversionExpressionView inversion)
    {
        switch (inversion)
        {
            case IComparisonExpressionView comparison:
                switch (comparison)
                {
                    case IBitwiseOrExpressionView bitwiseOr:
                        fillWithExpression(bitwiseOr);
                        break;

                    case ComparisonView compare:
                        fillWithExpression(compare.First);
                        foreach (var comp in compare.Rest)
                        {
                            switch (comp)
                            {
                                case GtEqOperationView gtEq:
                                    fillWithExpression(gtEq.Right);
                                    break;

                                case IsNotOperationView isNot:
                                    fillWithExpression(isNot.Right);
                                    break;

                                case LtEqOperationView ltEq:
                                    fillWithExpression(ltEq.Right);
                                    break;

                                case NotInOperationView notIn:
                                    fillWithExpression(notIn.Right);
                                    break;

                                case InOperationView @in:
                                    fillWithExpression(@in.Right);
                                    break;

                                case LtOperationView lt:
                                    fillWithExpression(lt.Right);
                                    break;

                                case EqOperationView eq:
                                    fillWithExpression(eq.Right);
                                    break;

                                case IsOperationView @is:
                                    fillWithExpression(@is.Right);
                                    break;

                                case GtOperationView gt:
                                    fillWithExpression(gt.Right);
                                    break;

                                case NotEqOperationView notEq:
                                    fillWithExpression(notEq.Right);
                                    break;

                                default:
                                    throw new UnreachableException();
                            }
                        }
                        break;

                    default:
                        throw new UnreachableException();
                }
                break;

            case InversionView inv:
                fillWithInversion(inv);
                break;

            default:
                throw new UnreachableException();
        }
    }

    private void fillWithBlock(BlockView block) => fillWithStatements(block.GetStatements());

    private void variableUsed(string name)
    {
        if (Symbols.TryGetValue(name, out var variable))
        {
            variable.Usage |= VariableUsages.Used;
        }
        else
        {
            if (tryGetFromAncestors(name, out variable))
            {
                if (variable.ScopeKind == DeclarationScopeKind.LocalFree && variable.Kind == VariableKind.Local)
                {
                    variable.Kind = VariableKind.Cell;

                    variable = new Variable
                    {
                        Name = name,
                        Kind = VariableKind.Free,
                        ScopeKind = DeclarationScopeKind.Closure,
                        Usage = VariableUsages.Used,
                    };
                    Symbols[name] = variable;
                }
                else
                {
                    variable = new Variable
                    {
                        Name = name,
                        Kind = VariableKind.GlobalImplicit,
                        Usage = VariableUsages.Used,
                        ScopeKind = DeclarationScopeKind.None,
                    };
                    Symbols[name] = variable;
                }
            }
            else
            {
                variable = new Variable
                {
                    Name = name,
                    Kind = VariableKind.GlobalImplicit,
                    Usage = VariableUsages.Used,
                    ScopeKind = DeclarationScopeKind.None,
                };
                Symbols[name] = variable;
            }
        }
    }

    private void variableAssigned(string name)
    {
        if (Symbols.TryGetValue(name, out var variable))
        {
            switch (variable.Kind)
            {
                case VariableKind.Local:
                case VariableKind.Cell:
                case VariableKind.GlobalExplicit:
                    variable.Usage |= VariableUsages.DefinedLocal;
                    break;

                case VariableKind.Free:
                    // If no 'nonlocal' was declared, make variable local
                    if (!variable.Usage.HasFlag(VariableUsages.DefinedNonlocal))
                    {
                        variable.Kind = VariableKind.Local;
                        variable.Usage &= VariableUsages.Used | VariableUsages.DefinedLocal;
                    }
                    break;

                case VariableKind.GlobalImplicit:
                    variable.Kind = VariableKind.Local;
                    variable.Usage = VariableUsages.DefinedLocal;
                    break;
            }
        }
        else
        {
            variable = new Variable
            {
                Name = name,
                Kind = VariableKind.Local,
                Usage = VariableUsages.DefinedLocal,
                ScopeKind = scopeKind,
            };
            Symbols[name] = variable;
        }
    }

    private void addChildTable(BlockView blockOfChild, List<Variable> parameters, string owner, bool isFunction)
    {
        var childTable = new SymbolTable(owner)
        {
            parent = this,
            scopeKind = isFunction ? DeclarationScopeKind.LocalFree : DeclarationScopeKind.LocalOnly,
        };
        childTable.fillSymbols(blockOfChild.GetStatements(), parameters);

        ChildSymbolTables.Add(childTable);
    }

    private bool tryGetFromAncestors(string name, [NotNullWhen(true)] out Variable? variable)
    {
        variable = null;
        if (parent == null)
            return false;

        if (parent.Symbols.TryGetValue(name, out variable))
            return true;
        else
            return parent.tryGetFromAncestors(name, out variable);
    }

    private void defer(FunctionDefView function)
    {
        List<Variable> parameters = function.Parameters?.Parameters
            .Select(param => param.GetParamName())
            .OfType<string>()
            .Select(name => new Variable()
            {
                Name = name,
                Kind = VariableKind.Local,
                Usage = VariableUsages.DefinedParam,
                ScopeKind = DeclarationScopeKind.LocalFree,
            })
            .ToList()
            ?? [];

        deferredChildren.Add((function.Block, parameters, function.Name.RawString, true));
    }

    private void defer(ClassDefView classDef)
        => deferredChildren.Add((classDef.Block, [], classDef.Name.RawString, true));
}
