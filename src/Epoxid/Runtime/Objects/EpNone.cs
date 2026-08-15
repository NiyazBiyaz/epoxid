namespace Epoxid.Runtime.Objects;

public class EpNone : EpObject
{
    public EpNone()
        : base(EpConstants.NoneType)
    {
    }

    internal static readonly EpType Type = new("NoneType", [EpConstants.Object], EpConstants.Type)
    {
        DunderBool = DunderBoolImplementation,
    };

    public override string ToString() => "None";

    internal static EpBool DunderBoolImplementation(EpObject self) => EpConstants.False;
}
