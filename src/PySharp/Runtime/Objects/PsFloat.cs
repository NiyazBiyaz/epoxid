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
}
