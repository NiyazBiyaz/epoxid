using System.Text;

namespace PySharp.Runtime.Objects;

public class PsString : PsObject
{
    internal readonly string Value;

    public PsString(string value)
        : base(PsConstants.Str)
    {
        Value = value;
    }

    public static explicit operator string(PsString str) => str.Value;
    public static explicit operator PsString(string str) => new(str);

    public override string ToString() => Value;

    public static PsString MultiplyString(PsString str, PsInteger times)
    {
        switch (times.Value)
        {
            case 1:
                return str;

            case 2:
                return (PsString)(str.Value + str.Value);

            case 3:
                return (PsString)(str.Value + str.Value + str.Value);

            default:
                var builder = new StringBuilder(capacity: str.Value.Length * (int)times.Value);
                for (int i = 0; i < times.Value; i++)
                {
                    builder.Append(str.Value);
                }

                return (PsString)builder.ToString();
        }
    }

    internal static PsString DunderAddImplementation(PsObject self, PsObject other)
    {
        var selfStr = (PsString)self;
        return other switch
        {
            PsString otherStr => (PsString)(selfStr.Value + otherStr.Value),
            _ => throw new Exception($"TypeError: cannot concatenate '{other.DunderClass.DunderName}' to 'str'")
        };
    }

    internal static PsString DunderMulImplementation(PsObject self, PsObject other)
    {
        var selfStr = (PsString)self;
        return other switch
        {
            PsInteger otherInt => MultiplyString(selfStr, otherInt),
            _ => throw new Exception($"TypeError: unsupported operand type(s) for *: 'str' and '{other.DunderClass.DunderName}'")
        };
    }
}
