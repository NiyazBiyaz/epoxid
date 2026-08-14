namespace Epoxid.Runtime.Objects;

public class EpNone : EpObject
{
    public EpNone()
        : base(EpConstants.NoneType)
    {
    }

    public override string ToString() => "None";

    internal static EpBool DunderBoolImplementation(EpObject self) => EpConstants.False;
}
