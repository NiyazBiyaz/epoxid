using System.Globalization;

namespace PySharp.Runtime.Objects;

public class PsFloat : PsObject
{
    internal readonly double Value;

    public PsFloat(double value)
        : base(PsConstants.Float)
    {
        Value = value;
    }

    public static explicit operator double(PsFloat psFloat) => psFloat.Value;
    public static explicit operator PsFloat(double clrFloat) => new(clrFloat);

    public override string ToString()
        => Value == (long)Value
        ? Value.ToString() + ".0" // I have no clue how to add .0 to this stupid double when converting to string, so...
        : Value.ToString(CultureInfo.InvariantCulture);

    internal static PsObject DunderAddImplementation(PsObject self, PsObject other)
    {
        var selfFloat = (PsFloat)self;
        return other switch
        {
            PsFloat otherFloat => (PsFloat)(selfFloat.Value + otherFloat.Value),
            PsInteger otherInt => (PsFloat)(selfFloat.Value + otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for +: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderSubImplementation(PsObject self, PsObject other)
    {
        var selfFloat = (PsFloat)self;
        return other switch
        {
            PsFloat otherFloat => (PsFloat)(selfFloat.Value - otherFloat.Value),
            PsInteger otherInt => (PsFloat)(selfFloat.Value - otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderMulImplementation(PsObject self, PsObject other)
    {
        var selfFloat = (PsFloat)self;
        return other switch
        {
            PsFloat otherFloat => (PsFloat)(selfFloat.Value * otherFloat.Value),
            PsInteger otherInt => (PsFloat)(selfFloat.Value * otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderTrueDivImplementation(PsObject self, PsObject other)
    {
        var selfFloat = (PsFloat)self;
        return other switch
        {
            PsFloat otherFloat => (PsFloat)(selfFloat.Value / otherFloat.Value),
            PsInteger otherInt => (PsFloat)(selfFloat.Value / otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for /: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsObject DunderPowImplementation(PsObject self, PsObject other)
    {
        var selfFloat = (PsFloat)self;
        return other switch
        {
            PsFloat otherFloat => (PsFloat)double.Pow(selfFloat.Value, otherFloat.Value),
            PsInteger otherInt => (PsFloat)double.Pow(selfFloat.Value, otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for **: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    internal static PsBool DunderBoolImplementation(PsObject self)
        => ((PsFloat)self).Value != 0d ? PsConstants.True : PsConstants.False;

    internal static new PsBool DunderEqImplementation(PsObject self, PsObject other)
    {
        var selfF = (PsFloat)self;
        return other switch
        {
            PsFloat otherF => (PsBool)(selfF.Value == otherF.Value),
            PsInteger otherI => (PsBool)(selfF.Value == otherI.Value),
            // Maybe another types exists that float can interact with, idk actually, but CPython doing just False
            _ => (PsBool)false,
        };
    }

    internal static new PsBool DunderNeImplementation(PsObject self, PsObject other)
    {
        var selfF = (PsFloat)self;
        return other switch
        {
            PsFloat otherF => (PsBool)(selfF.Value != otherF.Value),
            PsInteger otherI => (PsBool)(selfF.Value != otherI.Value),
            // As above, but True
            _ => (PsBool)true,
        };
    }
}
