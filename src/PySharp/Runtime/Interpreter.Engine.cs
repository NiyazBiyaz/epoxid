using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

public partial class Interpreter
{
    internal static PsObject CallFunction(PsObject func, PsObject args, PsObject? kwargs)
    {
        if (func.DunderClass.DunderCall == null)
        {
            throw new Exception($"TypeError: type '{func.DunderClass.DunderName}' is not callable.");
        }

        return func.DunderClass.DunderCall(func, args, kwargs);
    }

    internal static PsObject AddObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderAdd == null)
        {
            // TODO: __radd__
            throw new Exception($"TypeError: cannot add '{right.DunderClass.DunderName}' object to '{left.DunderClass.DunderName}' object.");
        }

        return left.DunderClass.DunderAdd(left, right);
    }

    internal static PsObject SubtractObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderSub == null)
        {
            // TODO: __rsub__
            throw new Exception($"TypeError: cannot subtract '{right.DunderClass.DunderName}' object from '{left.DunderClass.DunderName}' object.");
        }

        return left.DunderClass.DunderSub(left, right);
    }

    internal static PsObject MultiplyObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderMul == null)
        {
            // TODO: __rmul__
            throw new Exception($"TypeError: cannot multiply '{left.DunderClass.DunderName}' object by '{right.DunderClass.DunderName}' object.");
        }

        return left.DunderClass.DunderMul(left, right);
    }

    internal static PsObject TrueDivideObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderTrueDiv == null)
        {
            // TODO: __rtruediv__
            throw new Exception($"TypeError: cannot divide '{left.DunderClass.DunderName}' object by '{right.DunderClass.DunderName}' object.");
        }

        return left.DunderClass.DunderTrueDiv(left, right);
    }

    internal static PsObject PowerObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderPow == null)
        {
            // TODO: __rpow__
            throw new Exception($"TypeError: cannot power '{left.DunderClass.DunderName}' object to '{right.DunderClass.DunderName}' object.");
        }

        return left.DunderClass.DunderPow(left, right);
    }
}
