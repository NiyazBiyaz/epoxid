using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.Runtime;

internal static class Core
{
    internal static EpObject CallObject(EpObject callable, ReadOnlySpan<EpObject> args)
    {
        if (callable is not EpBaseFunction function)
        {
            if (callable.DunderClass.DunderCall == null)
            {
                throw new ArgumentException($"Object {callable.DunderClass.DunderName} is not callable.");
            }

            return callable.DunderClass.DunderCall(callable, args);
        }

        var descr = function.ParamsDescription;

        if (!descr.ArgumentsAreValid(args.Length, [], out string? message))
        {
            throw new Exception($"TypeError: {string.Format(message, function.QualName)}");
        }

        switch (function)
        {
            case EpBuiltinFunction builtin:
                if (builtin.FrameCall == null)
                {
                    throw new ArgumentException("Invalid function object: underlying method is not set");
                }

                return builtin.FrameCall(args);

            case EpFunction func:
            {
                var eng = Engine.CurrentEngine;

                if (func.Environment == null)
                {
                    throw new ArgumentException("Given function object doesn't have Environment bound on.", nameof(callable));
                }

                return eng.RunCode(func.Code, args, func.Environment.Value);
            }

            default:
                throw new UnreachableException($"Unknown function type: {function}");
        }
    }

    internal static EpTuple MethodResolutionOrder(EpType type) => [.. getMro(type)];

    private static List<EpType> getMro(EpType type)
    {
        // TODO: reduce allocations
        List<EpType> result;
        switch (type.DunderBases.Length)
        {
            case 0:
                result = [type];
                break;

            case 1:
                result = [type, .. getMro(type.DunderBases[0])];
                break;

            default:
            {
                // Simple C3 linearization algorithm implementation
                // TODO: use reversed lists, because now it would do O(n) for each linearization

                // Get linearizations for the all bases
                List<EpType>[] basesMro = new List<EpType>[type.DunderBases.Length];
                for (int i = 0; i < type.DunderBases.Length; i++)
                {
                    basesMro[i] = getMro(type.DunderBases[i]);
                }

                result = [type];

                // While any base linearizations contains elements, can add to the result
                bool hasAny = true;
                while (hasAny)
                {
                    EpType? candidate = null;
                    foreach (var mro in basesMro)
                    {
                        if (mro.Count == 0)
                            continue;

                        // Peak first element of the first mro.
                        var head = mro[0];

                        // If peaked head appears in the tails of another mros, it cannot be used
                        bool notInAnyTail = true;
                        foreach (var tail in basesMro)
                        {
                            if (tail.IndexOf(head, 1) != -1)
                            {
                                notInAnyTail = false;
                                break;
                            }
                        }
                        if (notInAnyTail)
                        {
                            candidate = head;
                            break;
                        }
                    }

                    if (candidate == null)
                        throw new MroConstructingException(type.DunderName);

                    result.Add(candidate);
                    foreach (var mro in basesMro)
                    {
                        // Remove selected item from the heads of the all mros
                        if (mro[0] == candidate)
                            mro.RemoveAt(0);
                    }
                    hasAny = basesMro.Any(mro => mro.Count > 0);
                }

                break;
            }
        }

        return result;
    }

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

    internal static EpObject ModuleObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderMod == null)
        {
            // TODO: __rtruediv__
            throw new Exception($"TypeError: unsupported operand type(s) for %: '{left.DunderClass.DunderName}' and '{right.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderMod(left, right);
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

    internal static bool ConvertToBool(EpObject obj)
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

    internal static EpObject EquateObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderEq == null)
        {
            throw new Exception($"TypeError: unsupported operand type for ==: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderEq(left, right);
    }

    internal static EpObject NegativeEquateObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderNe == null)
        {
            throw new Exception($"TypeError: unsupported operand type for !=: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderNe(left, right);
    }

    internal static EpObject CompareLessThanObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderLt == null)
        {
            throw new Exception($"TypeError: unsupported operand type for <: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderLt(left, right);
    }

    internal static EpObject CompareGreaterThanObjects(EpObject left, EpObject right)
    {
        if (left.DunderClass.DunderGt == null)
        {
            throw new Exception($"TypeError: unsupported operand type for >: '{left.DunderClass.DunderName}'");
        }

        return left.DunderClass.DunderGt(left, right);
    }

    internal static bool TryInPlaceAdd(EpObject target, EpObject right, [NotNullWhen(true)] out EpObject? result)
    {
        if (target.DunderClass.DunderIAdd == null)
        {
            result = null;
            return false;
        }

        result = target.DunderClass.DunderIAdd(target, right);
        return true;
    }

    internal static bool TryInPlaceSubtract(EpObject target, EpObject right, [NotNullWhen(true)] out EpObject? result)
    {
        if (target.DunderClass.DunderISub == null)
        {
            result = null;
            return false;
        }

        result = target.DunderClass.DunderISub(target, right);
        return true;
    }

    internal static bool TryInPlaceMultiply(EpObject target, EpObject right, [NotNullWhen(true)] out EpObject? result)
    {
        if (target.DunderClass.DunderIMul == null)
        {
            result = null;
            return false;
        }

        result = target.DunderClass.DunderIMul(target, right);
        return true;
    }

    internal static bool TryInPlaceTrueDivide(EpObject target, EpObject right, [NotNullWhen(true)] out EpObject? result)
    {
        if (target.DunderClass.DunderITrueDiv == null)
        {
            result = null;
            return false;
        }

        result = target.DunderClass.DunderITrueDiv(target, right);
        return true;
    }

    internal static bool TryInPlaceModule(EpObject target, EpObject right, [NotNullWhen(true)] out EpObject? result)
    {
        if (target.DunderClass.DunderIMod == null)
        {
            result = null;
            return false;
        }

        result = target.DunderClass.DunderIMod(target, right);
        return true;
    }

    internal static bool TryInPlacePower(EpObject target, EpObject right, [NotNullWhen(true)] out EpObject? result)
    {
        if (target.DunderClass.DunderIPow == null)
        {
            result = null;
            return false;
        }

        result = target.DunderClass.DunderIPow(target, right);
        return true;
    }

    internal static EpObject GetAttribute(EpObject obj, EpObject attributeName)
        => obj.DunderClass.DunderGetAttribute(obj, attributeName);

    internal static void SetAttribute(EpObject obj, EpObject attributeName, EpObject value)
        => obj.DunderClass.DunderSetAttr(obj, attributeName, value);
}
