namespace Epoxid.SyntaxAnalysis.Tokens;

public enum TokenizerError
{
    NoError,
    InvalidLineContinuation,
    InvalidLiteral,
    IndentationError,
    CharacterError,
    TooLongInterpolationExpression,
    UnclosedInterpolationExpression,
}
