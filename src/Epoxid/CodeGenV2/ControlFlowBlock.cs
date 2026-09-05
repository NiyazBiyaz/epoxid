using System.Diagnostics;

namespace Epoxid.CodeGenV2;

internal class ControlFlowBlock
{
    public List<IntermediateInstruction> Instructions { get; } = [];

    public IntermediateInstruction EndInstruction
    {
        get
        {
            Debug.Assert(Instructions.Count > 0);

            return Instructions[^1];
        }
    }

    public ControlFlowBlock? Next => EndInstruction.JumpLabel?.Target;
}
