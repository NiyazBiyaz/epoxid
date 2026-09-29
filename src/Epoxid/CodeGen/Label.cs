namespace Epoxid.CodeGen;

internal class Label
{
    public ControlFlowBlock? Target { get; set; }

    public int BranchUsageCount { get; set; } = 0;
}
