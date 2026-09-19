using System.Collections.Immutable;
using System.Diagnostics;

namespace Epoxid.CodeGenV2;

[DebuggerDisplay("Block {Id}")]
internal class ControlFlowBlock(int id)
{
    private Label? branchedLabel;

    private readonly List<Label> referrerLabels = [];

    public int Id { get; } = id;

    public int BranchedReferenceCount => referrerLabels.Sum(rl => rl.BranchUsageCount);

    public ImmutableArray<int> BranchLevel { get; set; }

    public List<IntermediateInstruction> Instructions { get; } = [];

    public IntermediateInstruction LastInstruction
    {
        get
        {
            Debug.Assert(Instructions.Count > 0);

            return Instructions[^1];
        }
    }

    public IntermediateInstruction FirstInstruction
    {
        get
        {
            Debug.Assert(Instructions.Count > 0);

            return Instructions[0];
        }
    }

    public ControlFlowBlock? Next { get; set; }

    public ControlFlowBlock? Branched => branchedLabel?.Target;

    public override int GetHashCode() => Id.GetHashCode();

    public void SetBranchedLabel(Label branchedLabel) => this.branchedLabel = branchedLabel;

    public void AddReferredLabel(Label label) => referrerLabels.Add(label);

    public bool Precedes(ControlFlowBlock other) => BranchLevel.Precedes(other.BranchLevel);

    public override string ToString() => string.Join('\n', Instructions);
}

file static class ImmutableArrayExtensions
{
    extension(ImmutableArray<int> branchLevel)
    {
        public bool Precedes(ImmutableArray<int> other)
        {
            var levelSpan = branchLevel.AsSpan();
            var otherSpan = other.AsSpan();

            int firstDifferenceIndex = levelSpan.CommonPrefixLength(otherSpan);

            if (levelSpan.Length == firstDifferenceIndex)
            {
                if (branchLevel.Length == other.Length)
                    return false;
                else
                    return true;
            }
            else if (otherSpan.Length == firstDifferenceIndex)
            {
                if (levelSpan.Length != firstDifferenceIndex)
                    return false;
            }

            return levelSpan[firstDifferenceIndex] < otherSpan[firstDifferenceIndex];
        }
    }
}
