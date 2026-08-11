namespace PySharp.SyntaxAnalysis;

// TODO: class validator

/// <summary>
/// Class to bind names of the <i>code block</i> and validate it.
/// <br/><br/>
/// <seealso href="https://docs.python.org/3/reference/executionmodel.html#structure-of-a-program"/>
/// </summary>
internal class CodeBlockValidator(IEnumerable<IStatementView> statements)
{
    public List<string> Variables { get; init; } = [];

    private readonly IEnumerable<IStatementView> blockStatements = statements;

    public ValidationResult ValidateCode()
    {
        foreach (var statement in blockStatements)
        {
            switch (statement)
            {
                case ICompoundStatementView:

                    break;

                case SeparatedSimpleStatementsView separated:
                    foreach (var stmt in separated.Values)
                    {
                        var result = validateSimpleStatement(stmt);
                        if (result is not ValidationResult.Success)
                            return result;
                    }
                    break;

                case SingleSimpleStatementView single:
                {
                    var result = validateSimpleStatement(single.Value);
                    if (result is not ValidationResult.Success)
                        return result;
                    break;
                }

                default:
                    throw new NotImplementedException();
            }
        }

        return ValidationResult.ResultSuccess;
    }

    private ValidationResult validateSimpleStatement(ISimpleStatementView statement)
    {
        switch (statement)
        {
            case IImportStatementView:
            case IStarExpressionVariantView:
            case DeleteStatementView:
            case ReturnStatementView:
            case AssignmentView:
            case AssertStatementView:
            case GlobalStatementView:
            case NonlocalStatementView:
            case RaiseStatementView:
            case YieldStatementView:
            case TypeAliasView:
            case BreakStatementView:
            case ContinueStatementView:

            case PassStatementView:
                break;

            default:
                throw new InvalidOperationException();
        }

        return ValidationResult.ResultSuccess;
    }
}
