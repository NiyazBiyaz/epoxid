namespace Epoxid.Runtime.Objects;

public class EpEllipsis : EpObject
{
    public EpEllipsis()
        : base(EpConstants.EllipsisType)
    {
    }

    internal static readonly EpType Type = new("EllipsisType", [EpConstants.Object], EpConstants.Type);

    public override string ToString() => "Ellipsis";
}
