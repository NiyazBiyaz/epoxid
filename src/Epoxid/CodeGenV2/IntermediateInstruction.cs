using Epoxid.VM;

namespace Epoxid.CodeGenV2;

internal record IntermediateInstruction
{
    public required Opcode Opcode { get; init; }

    public Register? Destination { get; init; } = null;
    public Register? Source1 { get; init; } = null;
    public Register? Source2 { get; init; } = null;

    public int? ImmediateValue { get; init; } = null;
    public int? ArgCount { get; init; } = null;

    public Label? JumpLabel
    {
        get;
        init
        {
            if (value != null && !Opcode.IsBranch)
                throw new InvalidOperationException("Only branch opcodes can have jumps");

            if (value == null && Opcode.IsBranch)
                throw new InvalidOperationException("Branch opcodes should have JumpToBlock value set");

            field = value;
        }
    } = null;
}
