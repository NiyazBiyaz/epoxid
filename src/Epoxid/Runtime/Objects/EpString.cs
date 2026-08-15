using System.Text;

namespace Epoxid.Runtime.Objects;

public class EpString : EpObject
{
    internal readonly string Value;

    public EpString(string value)
        : base(EpConstants.Str)
    {
        Value = value;
    }

    public static explicit operator string(EpString str) => str.Value;
    public static explicit operator EpString(string str) => new(str);

    internal static readonly EpType Type = new("str", [EpConstants.Object], EpConstants.Type)
    {
        DunderAdd = DunderAddImplementation,
        DunderMul = DunderMulImplementation,
        DunderLen = DunderLenImplementation,
        DunderEq = DunderEqImplementation,
        DunderNe = DunderNeImplementation,
    };

    public override string ToString() => Value;

    public static EpString MultiplyString(EpString str, EpInteger times)
    {
        switch (times.Value)
        {
            case 1:
                return str;

            case 2:
                return (EpString)(str.Value + str.Value);

            case 3:
                return (EpString)(str.Value + str.Value + str.Value);

            default:
                var builder = new StringBuilder(capacity: str.Value.Length * (int)times.Value);
                for (int i = 0; i < times.Value; i++)
                {
                    builder.Append(str.Value);
                }

                return (EpString)builder.ToString();
        }
    }

    internal static EpString DunderAddImplementation(EpObject self, EpObject other)
    {
        var selfStr = (EpString)self;
        return other switch
        {
            EpString otherStr => (EpString)(selfStr.Value + otherStr.Value),
            _ => throw new Exception($"TypeError: cannot concatenate '{other.DunderClass.DunderName}' to 'str'")
        };
    }

    internal static EpString DunderMulImplementation(EpObject self, EpObject other)
    {
        var selfStr = (EpString)self;
        return other switch
        {
            EpInteger otherInt => MultiplyString(selfStr, otherInt),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'str' and '{other.DunderClass.DunderName}'")
        };
    }

    internal static EpInteger DunderLenImplementation(EpObject self) => (EpInteger)((EpString)self).Value.Length;

    internal static new EpBool DunderEqImplementation(EpObject self, EpObject other)
    {
        var selfS = (EpString)self;
        return other switch
        {
            EpString otherS => (EpBool)selfS.Value.SequenceEqual(otherS.Value),
            // Maybe another types exists that str can interact with, idk actually, but CPython doing just False
            _ => (EpBool)false,
        };
    }

    internal static new EpBool DunderNeImplementation(EpObject self, EpObject other)
    {
        var selfS = (EpString)self;
        return other switch
        {
            EpString otherS => (EpBool)!selfS.Value.SequenceEqual(otherS.Value),
            // As above, but True
            _ => (EpBool)true,
        };
    }
}
