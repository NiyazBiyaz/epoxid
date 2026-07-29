namespace PySharp.Runtime.Objects;

public class PsBool : PsInteger
{
    internal new bool Value => base.Value != 0;

    public PsBool(bool value)
        : base(PsConstants.Bool, value ? 1 : 0)
    {
    }

    public static explicit operator bool(PsBool psBool) => psBool.Value;
    public static explicit operator PsBool(bool clrBool) => new(clrBool);

    public override string ToString() => Value.ToString();
}
