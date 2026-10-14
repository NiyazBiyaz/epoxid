using System.Globalization;
using System.Numerics;

namespace Epoxid.Runtime.Objects;

public class EpInteger : EpObject
{
    // 'int' because in compilable version it's 32-bit, so it is better for consistency
    internal readonly int Value;

    public EpInteger(int value)
        : base(Int)
    {
        Value = value;
    }

    // For bool inheritance.
    protected EpInteger(EpType derivedType, int value)
        : base(derivedType)
    {
        Value = value;
    }

    public static explicit operator int(EpInteger integer) => integer.Value;
    public static explicit operator EpInteger(int integer) => new(integer);

    public static bool operator ==(EpInteger left, int right) => left.Value == right;
    public static bool operator !=(EpInteger left, int right) => left.Value != right;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    internal static readonly EpType Int = new("int", [Object], EpType.Type)
    {
        DunderAdd = dunderAddImplementation,
        DunderSub = dunderSubImplementation,
        DunderMul = dunderMulImplementation,
        DunderTrueDiv = dunderTrueDivImplementation,
        DunderMod = dunderModImplementation,
        DunderPow = dunderPowImplementation,
        DunderBool = dunderBoolImplementation,
        DunderEq = dunderEqImplementation,
        DunderNe = dunderNeImplementation,
        DunderGt = dunderGtImplementation,
        DunderLt = dunderLtImplementation,
    };

    private static EpObject dunderAddImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpInteger)(selfInt.Value + otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value + otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for +: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpObject dunderSubImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpInteger)(selfInt.Value - otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value - otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpObject dunderMulImplementation(EpObject self, EpObject other)
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

    private static EpObject dunderTrueDivImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpFloat)((double)selfInt.Value / otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value / otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpObject dunderModImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        return other switch
        {
            EpInteger otherInt => (EpInteger)(selfInt.Value % otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfInt.Value % otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for %: 'int' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpObject dunderPowImplementation(EpObject self, EpObject other)
    {
        var selfInt = (EpInteger)self;
        switch (other)
        {
            case EpInteger otherInt when otherInt.Value > 0:
            {
                int result = (int)BigInteger.Pow(selfInt.Value, otherInt.Value);
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

    private static EpBool dunderBoolImplementation(EpObject self)
        => ((EpInteger)self).Value != 0 ? EpBool.True : EpBool.False;

    private static EpBool dunderEqImplementation(EpObject self, EpObject other)
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

    private static EpBool dunderNeImplementation(EpObject self, EpObject other)
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

    private static EpBool dunderLtImplementation(EpObject self, EpObject other)
    {
        var selfI = (EpInteger)self;
        return other switch
        {
            EpInteger otherI => (EpBool)(selfI.Value < otherI.Value),
            EpFloat otherF => (EpBool)(selfI.Value < otherF.Value),
            _ => throw new Exception($"TypeError: '<' not supported between instances of 'int' and '{other.DunderClass.DunderName}'")
        };
    }

    private static EpBool dunderGtImplementation(EpObject self, EpObject other)
    {
        var selfI = (EpInteger)self;
        return other switch
        {
            EpInteger otherI => (EpBool)(selfI.Value > otherI.Value),
            EpFloat otherF => (EpBool)(selfI.Value > otherF.Value),
            _ => throw new Exception($"TypeError: '>' not supported between instances of 'int' and '{other.DunderClass.DunderName}'")
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
