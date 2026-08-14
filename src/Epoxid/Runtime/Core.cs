using System.Diagnostics;
using Epoxid.Runtime.Objects;

namespace Epoxid.Runtime;

internal static class Core
{
    internal static EpObject CallFunction(EpObject funcObject, ReadOnlySpan<EpObject> args)
    {
        var descr = ((EpBaseFunction)funcObject).ParamsDescription;

        if (!descr.ArgumentsAreValid(args.Length, [], out string? message))
        {
            throw new Exception($"TypeError: {string.Format(message, ((EpBaseFunction)funcObject).QualName)}");
        }

        switch (funcObject)
        {
            case EpBuiltinFunction builtin:
                if (builtin.FrameCall == null)
                {
                    if (builtin.FrameKeywordCall == null)
                        throw new ArgumentException("Invalid function object: can't find any underlying function");

                    return builtin.FrameKeywordCall(args, EpDict.Empty);
                }

                return builtin.FrameCall(args);

            case EpFunction userFunc:
                throw new NotImplementedException();

            default:
                throw new ArgumentException("Argument is not callable Epoxid object");
        }
    }

    internal static EpObject CallKeywordFunction(EpObject funcObject, ReadOnlySpan<EpObject> args, EpObject kwargs)
    {
        if (kwargs is not EpDict kwDict)
        {
            throw new ArgumentException("Keyword arguments is not Epoxid dictionary.");
        }

        var descr = ((EpBaseFunction)funcObject).ParamsDescription;

        if (!descr.ArgumentsAreValid(args.Length, [], out string? message))
        {
            throw new Exception($"TypeError: {string.Format(message, ((EpBaseFunction)funcObject).QualName)}");
        }

        switch (funcObject)
        {
            case EpBuiltinFunction builtin:
                Debug.Assert(builtin.FrameKeywordCall != null);

                return builtin.FrameKeywordCall(args, kwDict);

            case EpFunction userFunc:
                throw new NotImplementedException();

            default:
                throw new ArgumentException("Argument is not callable Epoxid object");
        }
    }

    // TODO: MRO

    internal static EpObject AddObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderAdd == null)
        {
            // TODO: __radd__
            throw new Exception($"TypeError: unsupported operand type(s) for +: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderAdd(left, right);
    }

    internal static EpObject SubtractObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderSub == null)
        {
            // TODO: __rsub__
            throw new Exception($"TypeError: unsupported operand type(s) for -: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderSub(left, right);
    }

    internal static EpObject MultiplyObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderMul == null)
        {
            // TODO: __rmul__
            throw new Exception($"TypeError: unsupported operand type(s) for *: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderMul(left, right);
    }

    internal static EpObject TrueDivideObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderTrueDiv == null)
        {
            // TODO: __rtruediv__
            throw new Exception($"TypeError: unsupported operand type(s) for /: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderTrueDiv(left, right);
    }

    internal static EpObject PowerObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderPow == null)
        {
            // TODO: __rpow__
            throw new Exception($"TypeError: unsupported operand type(s) for **: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderPow(left, right);
    }

    internal static bool ConvertBool(EpObject obj)
    {
        if (obj.DunderClass.DunderBool != null)
        {
            var result = obj.DunderClass.DunderBool(obj);
            return (bool)(EpBool)result;
        }
        if (obj.DunderClass.DunderLen != null)
        {
            var result = obj.DunderClass.DunderLen(obj);
            return ((EpInteger)result).Value != 0;
        }

        return true;
    }

    internal static EpObject EqualObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderEq == null)
        {
            throw new Exception($"TypeError: unsupported operand type for ==: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderEq(left, right);
    }

    internal static EpObject NotEqualObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderNe == null)
        {
            throw new Exception($"TypeError: unsupported operand type for ==: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderNe(left, right);
    }
}
