namespace Epoxid.Runtime.Objects;

public class EpNone : EpObject
{
    public EpNone()
        : base(NoneType)
    {
    }

    internal static readonly EpType NoneType = new("NoneType", [Object], EpType.Type)
    {
        DunderBool = dunderBoolImplementation,
    };

    public static readonly EpNone None = new();

    public override string ToString() => "None";

    private static EpBool dunderBoolImplementation(EpObject self) => EpBool.False;
}
