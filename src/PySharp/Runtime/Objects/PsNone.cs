namespace PySharp.Runtime.Objects;

public class PsNone : PsObject
{
    public PsNone()
        : base(PsConstants.NoneType)
    {
    }

    public override string ToString() => "None";
}
