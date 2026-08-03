namespace PySharp.Runtime.Objects;

public class PsNone : PsObject
{
    public PsNone()
        : base(PsConstants.NoneType)
    {
    }

    public override string ToString() => "None";

    internal static PsBool DunderBoolImplementation(PsObject self) => PsConstants.False;
}
