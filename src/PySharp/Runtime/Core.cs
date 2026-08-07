using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

internal static class Core
{
    internal static PsObject CallFunction(PsObject func, PsObject args, PsObject kwargs)
    {
        if (func.DunderClass.DunderCall == null)
        {
            throw new Exception($"TypeError: type '{func.DunderClass.DunderName}' is not callable.");
        }

        var descr = ((PsBaseFunction)func).ParamsDescription;

        if (!descr.IsArgumentsAreValid((PsTuple)args, (PsDict)kwargs, out string? message))
        {
            throw new Exception($"TypeError: {string.Format(message, ((PsBaseFunction)func).QualName)}");
        }

        return func.DunderClass.DunderCall(func, args, kwargs);
    }

    // TODO: MRO

    internal static PsObject AddObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderAdd == null)
        {
            // TODO: __radd__
            throw new Exception($"TypeError: unsupported operand type(s) for +: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderAdd(left, right);
    }

    internal static PsObject SubtractObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderSub == null)
        {
            // TODO: __rsub__
            throw new Exception($"TypeError: unsupported operand type(s) for -: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderSub(left, right);
    }

    internal static PsObject MultiplyObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderMul == null)
        {
            // TODO: __rmul__
            throw new Exception($"TypeError: unsupported operand type(s) for *: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderMul(left, right);
    }

    internal static PsObject TrueDivideObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderTrueDiv == null)
        {
            // TODO: __rtruediv__
            throw new Exception($"TypeError: unsupported operand type(s) for /: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderTrueDiv(left, right);
    }

    internal static PsObject PowerObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderPow == null)
        {
            // TODO: __rpow__
            throw new Exception($"TypeError: unsupported operand type(s) for **: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderPow(left, right);
    }

    internal static bool ConvertBool(PsObject obj)
    {
        if (obj.DunderClass.DunderBool != null)
        {
            var result = obj.DunderClass.DunderBool(obj);
            return (bool)(PsBool)result;
        }
        if (obj.DunderClass.DunderLen != null)
        {
            var result = obj.DunderClass.DunderLen(obj);
            return ((PsInteger)result).Value != 0;
        }

        return true;
    }

    internal static PsObject EqualObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderEq == null)
        {
            throw new Exception($"TypeError: unsupported operand type for ==: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderEq(left, right);
    }

    internal static PsObject NotEqualObjects(PsObject left, PsObject right)
    {
        if (left.DunderClass.DunderNe == null)
        {
            throw new Exception($"TypeError: unsupported operand type for ==: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderNe(left, right);
    }
}
