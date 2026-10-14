using System.Text;
using Epoxid.SourceGeneration;

namespace Epoxid.Runtime.Objects;

[EpoxidType]
public partial class EpString : EpObject
{
    internal readonly string Value;

    public EpString(string value)
        : base(Str)
    {
        Value = value;
    }

    public static explicit operator string(EpString str) => str.Value;
    public static explicit operator EpString(string str) => new(str);

    internal static readonly EpType Str = new("str", [Object], EpType.Type)
    {
        DunderAdd = dunderAddImplementation,
        DunderMul = dunderMulImplementation,
        DunderLen = dunderLenImplementation,
        DunderEq = dunderEqImplementation,
        DunderNe = dunderNeImplementation,
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

    private static EpString dunderAddImplementation(EpObject self, EpObject other)
    {
        var selfStr = (EpString)self;
        return other switch
        {
            EpString otherStr => (EpString)(selfStr.Value + otherStr.Value),
            _ => throw new Exception($"TypeError: cannot concatenate '{other.DunderClass.DunderName}' to 'str'")
        };
    }

    private static EpString dunderMulImplementation(EpObject self, EpObject other)
    {
        var selfStr = (EpString)self;
        return other switch
        {
            EpInteger otherInt => MultiplyString(selfStr, otherInt),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'str' and '{other.DunderClass.DunderName}'")
        };
    }

    private static EpInteger dunderLenImplementation(EpObject self) => (EpInteger)((EpString)self).Value.Length;

    private static EpBool dunderEqImplementation(EpObject self, EpObject other)
    {
        var selfS = (EpString)self;
        return other switch
        {
            EpString otherS => (EpBool)selfS.Value.SequenceEqual(otherS.Value),
            // Maybe another types exists that str can interact with, idk actually, but CPython doing just False
            _ => (EpBool)false,
        };
    }

    private static EpBool dunderNeImplementation(EpObject self, EpObject other)
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
