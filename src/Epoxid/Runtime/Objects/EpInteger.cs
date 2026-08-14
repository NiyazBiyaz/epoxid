using System.Globalization;
using System.Numerics;

namespace Epoxid.Runtime.Objects;

public class EpInteger : EpObject
{
    // long is not that small as int and still fast/convenient to use.
    internal readonly long Value;

    public EpInteger(long value)
        : base(EpConstants.Int)
    {
        Value = value;
    }

    // For bool inheritance.
    protected EpInteger(EpType derivedType, long value)
        : base(derivedType)
    {
        Value = value;
    }

    public static explicit operator long(EpInteger integer) => integer.Value;
    public static explicit operator EpInteger(long integer) => new(integer);

    public static bool operator ==(EpInteger left, long right) => left.Value == right;
    public static bool operator !=(EpInteger left, long right) => left.Value != right;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    internal static EpObject DunderAddImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpInteger)(selfInt.Value + otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value + otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for +: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderSubImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpInteger)(selfInt.Value - otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value - otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderMulImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpInteger)(selfInt.Value * otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value * otherFloat.Value),
            EpString otherStr => EpString.MultiplyString(otherStr, selfInt),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderTrueDivImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpFloat)((double)selfInt.Value / otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value / otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderPowImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        switch (other)
        {
            case EpInteger otherInt when otherInt.Value > 0:
            {
                long result = checked((long)BigInteger.Pow(selfInt.Value, (int)otherInt.Value));
                return (EpInteger)result;
            }
            case EpInteger otherInt when otherInt.Value < 0:
            {
                double result = double.Pow(selfInt.Value, otherInt.Value);
                return (EpFloat)result;
            }
            case EpFloat otherFloat:
            {
                double result = double.Pow(selfInt.Value, otherFloat.Value);
                return (EpFloat)result;
            }
        }

        throw new Exception($"TypeError: unsupported operand type(s) for **: 'int' and '{other.DunderClass.DunderName}'");
    }

    internal static EpBool DunderBoolImplementation(EpObject self)
        => ((EpInteger)self).Value != 0 ? EpConstants.True : EpConstants.False;

    internal static new EpBool DunderEqImplementation(EpObject self, EpObject other)
    {
        var selfI = (EpInteger)self;
        return other switch
        {
            EpInteger otherI => (EpBool)(selfI.Value == otherI.Value),
            EpFloat otherF => (EpBool)(selfI.Value == otherF.Value),
            // Maybe another types exists that int can interact with, idk actually, but CPython doing just False
            _ => (EpBool)false,
        };
    }

    internal static new EpBool DunderNeImplementation(EpObject self, EpObject other)
    {
        var selfI = (EpInteger)self;
        return other switch
        {
            EpInteger otherI => (EpBool)(selfI.Value != otherI.Value),
            EpFloat otherF => (EpBool)(selfI.Value != otherF.Value),
            // As above, but True
            _ => (EpBool)true,
        };
    }

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Value);
        hash.Add(DunderClass);

        return hash.ToHashCode();
    }
}
