using System.Diagnostics;
using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

internal static class Core
{
    internal static PsObject CallFunction(PsObject funcObject, ReadOnlySpan<PsObject> args)
    {
        var descr = ((PsBaseFunction)funcObject).ParamsDescription;

        if (!descr.ArgumentsAreValid(args.Length, [], out string? message))
        {
            throw new Exception($"TypeError: {string.Format(message, ((PsBaseFunction)funcObject).QualName)}");
        }

        switch (funcObject)
        {
            case PsBuiltinFunction builtin:
                if (builtin.FrameCall == null)
                {
                    if (builtin.FrameKeywordCall == null)
                        throw new ArgumentException("Invalid function object: can't find any underlying function");

                    return builtin.FrameKeywordCall(args, PsDict.Empty);
                }

                return builtin.FrameCall(args);

            case PsFunction userFunc:
                throw new NotImplementedException();

            default:
                throw new ArgumentException("Argument is not callable Py# object");
        }
    }

    internal static PsObject CallKeywordFunction(PsObject funcObject, ReadOnlySpan<PsObject> args, PsObject kwargs)
    {
        if (kwargs is not PsDict kwDict)
        {
            throw new ArgumentException("Keyword arguments is not Py# dictionary.");
        }

        var descr = ((PsBaseFunction)funcObject).ParamsDescription;

        if (!descr.ArgumentsAreValid(args.Length, [], out string? message))
        {
            throw new Exception($"TypeError: {string.Format(message, ((PsBaseFunction)funcObject).QualName)}");
        }

        switch (funcObject)
        {
            case PsBuiltinFunction builtin:
                Debug.Assert(builtin.FrameKeywordCall != null);

                return builtin.FrameKeywordCall(args, kwDict);

            case PsFunction userFunc:
                throw new NotImplementedException();

            default:
                throw new ArgumentException("Argument is not callable Py# object");
        }
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
