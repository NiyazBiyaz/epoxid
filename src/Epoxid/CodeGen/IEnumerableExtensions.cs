using Epoxid.SyntaxAnalysis;

namespace Epoxid.CodeGen;

internal static class IEnumerableExtensions
{
    extension(IEnumerable<IStatementView> statements)
    {
        public IEnumerable<IStatementView> GetTopLevelStatements() => statements
            .SelectMany(extractStatements);

        private static IEnumerable<IStatementView> extractStatements(IStatementView statement) => statement switch
        {
            IfStatementView ifStmt => ifStmt.Block
                .GetStatements()
                .Concat(ifStmt.Elifs.SelectMany(elif => elif.Block.GetStatements()))
                .Concat(ifStmt.Else?.Block.GetStatements() ?? []),

            WhileStatementView whileStmt => whileStmt.Block
                .GetStatements()
                .Concat(whileStmt.Else?.Block.GetStatements() ?? []),

            // TODO: ForStatement, WithStatement, TryStatement

            var other => [other],
        };
    }
}
