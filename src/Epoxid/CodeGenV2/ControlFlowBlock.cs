using System.Diagnostics;

namespace Epoxid.CodeGenV2;

internal class ControlFlowBlock(int id)
{
    private Label? branchedLabel;

    public int Id { get; } = id;

    public int LabelsCount { get; set; } = 0;

    public int StartInstructionAddress { get; set; }

    public List<IntermediateInstruction> Instructions { get; } = [];

    public IntermediateInstruction EndInstruction
    {
        get
        {
            Debug.Assert(Instructions.Count > 0);

            return Instructions[^1];
        }
    }

    public ControlFlowBlock? Next { get; set; }

    public ControlFlowBlock? Branched => branchedLabel?.Target;

    public void SetBranchedLabel(Label branchedLabel) => this.branchedLabel = branchedLabel;

    public override string ToString() => string.Join('\n', Instructions);
}
