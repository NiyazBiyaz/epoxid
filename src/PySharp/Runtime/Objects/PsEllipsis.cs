namespace PySharp.Runtime.Objects;

public class PsEllipsis : PsObject
{
    public PsEllipsis()
        : base(PsConstants.EllipsisType)
    {
    }

    public override string ToString() => "Ellipsis";
}
