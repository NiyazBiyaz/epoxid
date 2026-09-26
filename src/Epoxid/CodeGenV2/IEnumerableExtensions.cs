using Epoxid.SyntaxAnalysis;

namespace Epoxid.CodeGenV2;

internal static class IEnumerableExtensions
{
    extension(IEnumerable<IStatementView> statements)
    {
        public IEnumerable<IStatementView> GetTopLevelStatements() => statements
            .SelectMany(extractStatements);

        private static IEnumerable<IStatementView> extractStatements(IStatementView statement) => statement switch
        {
            BlockView block => block.GetStatements().SelectMany(extractStatements),
            var other => [other],
        };
    }
}
