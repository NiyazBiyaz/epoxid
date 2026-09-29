namespace Epoxid.CodeGen;

internal class UsageSpan
{
    public int First { get; private set; } = -1;
    public int Last { get; private set; } = -1;

    public void AddUsagePoint(int pointNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pointNumber, nameof(pointNumber));
        if (pointNumber < First || pointNumber < Last)
            return;

        if (First == -1 || First == pointNumber)
        {
            First = pointNumber;
        }
        else
        {
            Last = pointNumber;
        }
    }

    public bool CollidesWith(UsageSpan other)
    {
        if (Last == -1 || other.Last == -1)
            return false;

        return int.Max(First, other.First) < int.Min(Last, other.Last);
    }

    public static readonly UsageSpan Full = new()
    {
        First = 0,
        Last = int.MaxValue
    };
}
