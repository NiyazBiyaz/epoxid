namespace Epoxid.Runtime.Objects;

public class EpEllipsis : EpObject
{
    public EpEllipsis()
        : base(EpConstants.EllipsisType)
    {
    }

    public override string ToString() => "Ellipsis";
}
