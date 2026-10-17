using System.Globalization;
using Epoxid.SyntaxAnalysis.Common;

namespace Epoxid.Runtime.Objects;

public class EpFloat : EpObject
{
    internal double Value { get; private set; }

    public EpFloat(double value)
        : base(Float)
    {
        Value = value;
    }

    public static explicit operator double(EpFloat psFloat) => psFloat.Value;
    public static explicit operator EpFloat(double clrFloat) => new(clrFloat);

    public override string ToString()
        => Value == (long)Value
        ? Value.ToString() + ".0" // I have no clue how to add .0 to this stupid double when converting to string, so...
        : Value.ToString(CultureInfo.InvariantCulture);

    internal static readonly EpType Float = new("float", [Object], EpType.Type)
    {
        DunderNew = dunderNewImplementation,
        DunderInit = dunderInitImplementation,
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

    private static EpFloat dunderNewImplementation(EpObject _) => new(0.0);

    private static EpNone dunderInitImplementation(EpObject self, ReadOnlySpan<EpObject> args)
    {
        if (self is not EpFloat selfFloat)
            throw new ArgumentException("'self' is not a float object");

        switch (args)
        {
            case []:
                break;

            case [EpString str]:
            {
                // TODO: maybe a 'TryX' method?
                double value = NumberParser.ParseFloat(str.Value);
                selfFloat.Value = value;
                break;
            }
            case [EpFloat fl]:
                selfFloat.Value = fl.Value;
                break;

            case [EpInteger integer]:
                selfFloat.Value = integer.Value;
                break;

            default:
                throw new NotImplementedException("Idk which error to put in here");
        }

        return EpNone.None;
    }

    private static EpFloat dunderAddImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value + otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value + otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for +: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpFloat dunderSubImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value - otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value - otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for -: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpFloat dunderMulImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value * otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value * otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpFloat dunderTrueDivImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)(selfFloat.Value / otherFloat.Value),
            EpInteger otherInt => (EpFloat)(selfFloat.Value / otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for /: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpFloat dunderModImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpInteger otherInt => (EpFloat)(selfFloat.Value % otherInt.Value),
            EpFloat otherFloat => (EpFloat)(selfFloat.Value % otherFloat.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for %: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpFloat dunderPowImplementation(EpObject self, EpObject other)
    {
        var selfFloat = (EpFloat)self;
        return other switch
        {
            EpFloat otherFloat => (EpFloat)double.Pow(selfFloat.Value, otherFloat.Value),
            EpInteger otherInt => (EpFloat)double.Pow(selfFloat.Value, otherInt.Value),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for **: 'float' and '{other.DunderClass.DunderName}'"),
        };
    }

    private static EpBool dunderBoolImplementation(EpObject self)
        => ((EpFloat)self).Value != 0d ? EpBool.True : EpBool.False;

    private static EpBool dunderEqImplementation(EpObject self, EpObject other)
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

    private static EpBool dunderNeImplementation(EpObject self, EpObject other)
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

    private static EpBool dunderLtImplementation(EpObject self, EpObject other)
    {
        var selfF = (EpFloat)self;
        return other switch
        {
            EpInteger otherI => (EpBool)(selfF.Value < otherI.Value),
            EpFloat otherF => (EpBool)(selfF.Value < otherF.Value),
            _ => throw new Exception($"TypeError: '<' not supported between instances of 'float' and '{other.DunderClass.DunderName}'")
        };
    }

    private static EpBool dunderGtImplementation(EpObject self, EpObject other)
    {
        var selfF = (EpFloat)self;
        return other switch
        {
            EpInteger otherI => (EpBool)(selfF.Value > otherI.Value),
            EpFloat otherF => (EpBool)(selfF.Value > otherF.Value),
            _ => throw new Exception($"TypeError: '>' not supported between instances of 'float' and '{other.DunderClass.DunderName}'")
        };
    }
}
