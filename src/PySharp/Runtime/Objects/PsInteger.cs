using System.Globalization;
using System.Numerics;

namespace PySharp.Runtime.Objects;

public class PsInteger : PsObject
{
    // long is not that small as int and still fast/convenient to use.
    internal readonly long Value;

    public PsInteger(long value)
        : base(PsConstants.Int)
    {
        Value = value;
    }

    // For bool inheritance.
    protected PsInteger(PsType derivedType, long value)
        : base(derivedType)
    {
        Value = value;
    }

    public static explicit operator long(PsInteger integer) => integer.Value;
    public static explicit operator PsInteger(long integer) => new(integer);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    internal static PsObject DunderAddImplementation(PsObject self, PsObject other)
    {
        var selfInt = (PsInteger)self;
        return other switch
        {
            PsInteger otherInt => (PsInteger)(selfInt.Value + otherInt.Value),
            PsFloat otherFloat => (PsFloat)(selfInt.Value + otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for +: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderSubImplementation(PsObject self, PsObject other)
    {
        var selfInt = (PsInteger)self;
        return other switch
        {
            PsInteger otherInt => (PsInteger)(selfInt.Value - otherInt.Value),
            PsFloat otherFloat => (PsFloat)(selfInt.Value - otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderMulImplementation(PsObject self, PsObject other)
    {
        var selfInt = (PsInteger)self;
        return other switch
        {
            PsInteger otherInt => (PsInteger)(selfInt.Value * otherInt.Value),
            PsFloat otherFloat => (PsFloat)(selfInt.Value * otherFloat.Value),
            PsString otherStr => PsString.MultiplyString(otherStr, selfInt),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderTrueDivImplementation(PsObject self, PsObject other)
    {
        var selfInt = (PsInteger)self;
        return other switch
        {
            PsInteger otherInt => (PsFloat)((double)selfInt.Value / otherInt.Value),
            PsFloat otherFloat => (PsFloat)(selfInt.Value / otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderPowImplementation(PsObject self, PsObject other)
    {
        var selfInt = (PsInteger)self;
        switch (other)
        {
            case PsInteger otherInt when otherInt.Value > 0:
            {
                long result = checked((long)BigInteger.Pow(selfInt.Value, (int)otherInt.Value));
                return (PsInteger)result;
            }
            case PsInteger otherInt when otherInt.Value < 0:
            {
                double result = double.Pow(selfInt.Value, otherInt.Value);
                return (PsFloat)result;
            }
            case PsFloat otherFloat:
            {
                double result = double.Pow(selfInt.Value, otherFloat.Value);
                return (PsFloat)result;
            }
        }

        throw new Exception($"TypeError: unsupported operand type(s) for **: 'int' and '{other.DunderClass.DunderName}'");
    }
}
