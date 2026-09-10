namespace Epoxid.CodeGenV2;

internal class UsageSpan
{
    public int First { get; private set; } = -1;
    public int Last { get; private set; } = -1;

    public void AddUsage(int newUsage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newUsage, nameof(newUsage));
        if (newUsage < First || newUsage < Last)
            throw new ArgumentOutOfRangeException(nameof(newUsage), "new usage has less lifetime than already set");

        if (First == -1 || First == newUsage)
        {
            First = newUsage;
        }
        else
        {
            Last = newUsage;
        }
    }

    public bool CollidesWith(UsageSpan other)
    {
        if (Last == -1 || other.Last == -1)
            return false;

        return int.Max(First, other.First) < int.Min(Last, other.Last);
    }
}
