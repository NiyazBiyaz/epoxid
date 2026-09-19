namespace Epoxid.CodeGenV2;

internal class Register : IEquatable<Register>
{
    public int? Address = null;

    public int? CallId { get; set; }

    public int? CallRelativeAddress { get; set; }

    public int? CallCount { get; set; }

    public OrderedDictionary<ControlFlowBlock, UsageSpan> BlocksUsages { get; } = [];

    public List<Register> LifetimeCollisions { get; } = [];

    public bool Equals(Register? other)
    {
        if (other == null)
        {
            return false;
        }

        if (Address != null && other.Address != null)
        {
            return Address == other.Address;
        }

        return ReferenceEquals(this, other);
    }

    public bool CollidesWith(Register other)
    {
        if (BlocksUsages.Count == 0 || other.BlocksUsages.Count == 0)
            return false;

        if (BlocksUsages.Count == 1 && other.BlocksUsages.Count == 1)
        {
            var thisBlockUsage = BlocksUsages.GetAt(0);
            var otherBlockUsage = other.BlocksUsages.GetAt(0);
            if (thisBlockUsage.Key == otherBlockUsage.Key)
            {
                return thisBlockUsage.Value.CollidesWith(otherBlockUsage.Value);
            }
        }

        var thisFirstBlock = BlocksUsages.GetAt(0).Key;
        var thisLastBlock = BlocksUsages.GetAt(BlocksUsages.Count - 1).Key;

        var otherFirstBlock = other.BlocksUsages.GetAt(0).Key;
        var otherLastBlock = other.BlocksUsages.GetAt(other.BlocksUsages.Count - 1).Key;

        if (thisFirstBlock == otherFirstBlock)
        {
            return true;
        }
        if (thisLastBlock == otherLastBlock)
        {
            return true;
        }

        int thisMostCommonPrefixLength = getMostCommonPrefixLength();
        int otherMostCommonPrefixLength = other.getMostCommonPrefixLength();

        bool thisFirstBlockIsDetermined = thisFirstBlock.BranchLevel.Length == thisMostCommonPrefixLength + 1;
        bool thisLastBlockIsDetermined = thisLastBlock.BranchLevel.Length == thisMostCommonPrefixLength + 1;

        bool otherFirstBlockIsDetermined = otherFirstBlock.BranchLevel.Length == otherMostCommonPrefixLength + 1;
        bool otherLastBlockIsDetermined = otherLastBlock.BranchLevel.Length == otherMostCommonPrefixLength + 1;

        if (thisLastBlock == otherFirstBlock && thisLastBlockIsDetermined && otherFirstBlockIsDetermined)
        {
            return BlocksUsages[thisLastBlock].CollidesWith(other.BlocksUsages[otherFirstBlock]);
        }
        else if (thisFirstBlock == otherLastBlock && thisFirstBlockIsDetermined && otherLastBlockIsDetermined)
        {
            return BlocksUsages[thisFirstBlock].CollidesWith(other.BlocksUsages[otherLastBlock]);
        }

        foreach (var otherBlock in other.BlocksUsages.Keys)
        {
            if (thisFirstBlock.Precedes(otherBlock) && otherBlock.Precedes(thisLastBlock))
                return true;
        }

        return false;
    }

    private int getMostCommonPrefixLength()
    {
        var left = BlocksUsages.GetAt(0).Key.BranchLevel.AsSpan();
        int mostCommonPrefixLength = default;
        for (int i = 1; i < BlocksUsages.Count; i++)
        {
            var right = BlocksUsages.GetAt(i).Key.BranchLevel.AsSpan();

            mostCommonPrefixLength = left.CommonPrefixLength(right);

            left = right[..mostCommonPrefixLength];
        }

        return mostCommonPrefixLength;
    }

    public int ColoringSortScore => LifetimeCollisions.Count + (CallId == null ? 0 : 1) * 8192;

    public required int Id { get; init; }
    public override string ToString() => $"r{Id}";
}
