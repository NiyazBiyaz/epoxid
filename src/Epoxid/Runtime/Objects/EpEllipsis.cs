namespace Epoxid.Runtime.Objects;

public class EpEllipsis : EpObject
{
    private EpEllipsis()
        : base(EllipsisType)
    {
    }

    internal static readonly EpType EllipsisType = new("EllipsisType", [Object], EpType.Type);

    public static readonly EpEllipsis Ellipsis = new();

    public override string ToString() => "Ellipsis";
}
