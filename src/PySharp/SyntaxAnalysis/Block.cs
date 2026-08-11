namespace PySharp.SyntaxAnalysis;

public partial class BlockView
{
    public IEnumerable<IStatementView> GetStatements() => this switch
    {
        OneLinedBlockView oneLined => [oneLined.Statements],
        IndentedBlockView indented => indented.Statements,
        _ => throw new InvalidOperationException(),
    };
}
