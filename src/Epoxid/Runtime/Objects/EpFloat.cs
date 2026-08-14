using System.Globalization;

namespace Epoxid.Runtime.Objects;

public class EpFloat : EpObject
{
    internal readonly double Value;

    public EpFloat(double value)
        : base(EpConstants.Float)
    {
        Value = value;
    }

    public static explicit operator double(EpFloat psFloat) => psFloat.Value;
    public static explicit operator EpFloat(double clrFloat) => new(clrFloat);

    public override string ToString()
        => Value == (long)Value
        ? Value.ToString() + ".0" // I have no clue how to add .0 to this stupid double when converting to string, so...
        : Value.ToString(CultureInfo.InvariantCulture);

    internal static EpObject DunderAddImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value + otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value + otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for +: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderSubImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value - otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value - otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderMulImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value * otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value * otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderTrueDivImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value / otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value / otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for /: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpObject DunderPowImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)double.Pow(selfFloat.Value, otherFloat.Value),
            EpInteger otherInt => (EpFloat)double.Pow(selfFloat.Value, otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for **: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static EpBool DunderBoolImplementation(EpObject self)
        => ((EpFloat)self).Value != 0d ? EpConstants.True : EpConstants.False;

    internal static new EpBool DunderEqImplementation(EpObject self, EpObject other)
    {
        var selfF = (EpFloat)self;
        return other switch
        {
            EpFloat otherF => (EpBool)(selfF.Value == otherF.Value),
            EpInteger otherI => (EpBool)(selfF.Value == otherI.Value),
            // Maybe another types exists that float can interact with, idk actually, but CPython doing just False
            _ => (EpBool)false,
        };
    }

    internal static new EpBool DunderNeImplementation(EpObject self, EpObject other)
    {
        var selfF = (EpFloat)self;
        return other switch
        {
            EpFloat otherF => (EpBool)(selfF.Value != otherF.Value),
            EpInteger otherI => (EpBool)(selfF.Value != otherI.Value),
            // As above, but True
            _ => (EpBool)true,
        };
    }
}
