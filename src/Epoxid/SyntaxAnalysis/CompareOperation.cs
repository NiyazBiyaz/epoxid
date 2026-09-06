using System.Diagnostics;

namespace Epoxid.SyntaxAnalysis;

public enum CompareOperation
{
    Equals,
    NotEquals,
    LessThan,
    LessThanEquals,
    GreaterThan,
    GreaterThanEquals,
    In,
    NotIn,
    Is,
    IsNot,
}

public partial class CompareOperationView
{
    public CompareOperation Operation => this switch
    {
        EqOperationView => CompareOperation.Equals,
        NotEqOperationView => CompareOperation.NotEquals,
        LtOperationView => CompareOperation.LessThan,
        LtEqOperationView => CompareOperation.LessThanEquals,
        GtOperationView => CompareOperation.GreaterThan,
        GtEqOperationView => CompareOperation.GreaterThanEquals,
        InOperationView => CompareOperation.In,
        NotInOperationView => CompareOperation.NotIn,
        IsOperationView => CompareOperation.Is,
        IsNotOperationView => CompareOperation.IsNot,
        _ => throw new UnreachableException(),
    };

    public IBitwiseOrExpressionView Operand => this switch
    {
        EqOperationView eq => eq.Right,
        NotEqOperationView neq => neq.Right,
        LtOperationView lt => lt.Right,
        LtEqOperationView ltEq => ltEq.Right,
        GtOperationView gt => gt.Right,
        GtEqOperationView gtEq => gtEq.Right,
        InOperationView @in => @in.Right,
        NotInOperationView notIn => notIn.Right,
        IsOperationView @is => @is.Right,
        IsNotOperationView isNot => isNot.Right,
        _ => throw new UnreachableException(),
    };
}
