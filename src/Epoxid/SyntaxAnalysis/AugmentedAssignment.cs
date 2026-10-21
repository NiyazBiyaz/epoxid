using System.Diagnostics;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.SyntaxAnalysis;

public enum AugmentedAssignment
{
    Add,
    Subtract,
    Multiply,
    MatrixMultiply,
    TrueDivide,
    FloorDivide,
    Module,
    Power,
    LeftShift,
    RightShift,
    BitwiseAnd,
    BitwiseXor,
    BitwiseOr,
}

public partial class AugmentedAssignmentView
{
    public AugmentedAssignment Operation => Operator.Type switch
    {
        TokenType.PlusEqual => AugmentedAssignment.Add,
        TokenType.MinusEqual => AugmentedAssignment.Subtract,
        TokenType.StarEqual => AugmentedAssignment.Multiply,
        TokenType.AtEqual => AugmentedAssignment.MatrixMultiply,
        TokenType.SlashEqual => AugmentedAssignment.TrueDivide,
        TokenType.DoubleSlashEqual => AugmentedAssignment.FloorDivide,
        TokenType.PercentEqual => AugmentedAssignment.Module,
        TokenType.DoubleStarEqual => AugmentedAssignment.Power,
        TokenType.LeftShiftEqual => AugmentedAssignment.LeftShift,
        TokenType.RightShiftEqual => AugmentedAssignment.RightShift,
        TokenType.AmpersandEqual => AugmentedAssignment.BitwiseAnd,
        TokenType.CircumflexEqual => AugmentedAssignment.BitwiseXor,
        TokenType.VertBarEqual => AugmentedAssignment.BitwiseOr,
        _ => throw new UnreachableException(),
    };
}
