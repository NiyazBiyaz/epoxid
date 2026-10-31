namespace Epoxid.Runtime.Objects;

public class EpBool : EpInteger
{
    internal new bool Value => base.Value != 0;

    private EpBool(bool value)
        : base(Bool, value ? 1 : 0)
    {
    }

    internal static readonly EpType Bool;

    static EpBool()
    {
        Bool = new("bool", [Int], EpType.Type)
        {
            DunderBool = dunderBoolImplementation,
        };
        Bool.FillSlots();

        True = new(true);
        False = new(false);
    }

    public static explicit operator bool(EpBool psBool) => psBool.Value;
    public static explicit operator EpBool(bool clrBool) => clrBool ? True : False;

    public static readonly EpBool True;
    public static readonly EpBool False;

    public override string ToString() => Value.ToString();

    private static EpBool dunderBoolImplementation(EpObject self) => (EpBool)self;
}
