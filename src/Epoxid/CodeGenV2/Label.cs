namespace Epoxid.CodeGenV2;

internal class Label
{
    public ControlFlowBlock? Target { get; set; }

    public int BranchUsageCount { get; set; } = 0;
}
