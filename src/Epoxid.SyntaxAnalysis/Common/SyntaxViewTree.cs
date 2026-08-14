using Epoxid.SyntaxAnalysis.Common.Ast;

namespace Epoxid.SyntaxAnalysis.Common;

public readonly struct SyntaxViewTree
{
    public required IRedView Root { get; init; }
    public required TextPositionMap PositionMap { get; init; }
}
