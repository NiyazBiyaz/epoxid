namespace Epoxid.Runtime.Objects;

public class EpBool : EpInteger
{
    internal new bool Value => base.Value != 0;

    public EpBool(bool value)
        : base(EpConstants.Bool, value ? 1 : 0)
    {
    }

    public static explicit operator bool(EpBool psBool) => psBool.Value;
    public static explicit operator EpBool(bool clrBool) => clrBool ? EpConstants.True : EpConstants.False;

    public override string ToString() => Value.ToString();

    internal new static EpBool DunderBoolImplementation(EpObject self) => (EpBool)self;
}
