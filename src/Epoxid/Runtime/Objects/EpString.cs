using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Epoxid.Runtime.Objects;

public class EpString : EpObject, IEquatable<EpString>
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

    public bool Equals(EpString? other)
    {
        if (other == null)
            return false;

        return other.Value == Value;
    }

    public override bool Equals(object? obj) => obj is EpString otherStr && Equals(otherStr);

    public override int GetHashCode() => Value.GetHashCode();
}

public class EpObjectAlternateComparer : IEqualityComparer<EpObject>, IAlternateEqualityComparer<string, EpObject>
{
    public EpObject Create(string alternate) => new EpString(alternate);

    public bool Equals(string alternate, EpObject other) => other is EpString str && str.Value == alternate;

    public bool Equals(EpObject? x, EpObject? y)
    {
        if (x == null || y == null)
            return false;

        return Core.ConvertToBool(Core.EquateObjects(x, y));
    }

    public int GetHashCode(string alternate) => alternate.GetHashCode();

    public int GetHashCode([DisallowNull] EpObject obj) => obj.GetHashCode(); // TODO: change to __hash__, maybe
}
