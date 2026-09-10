using System.Diagnostics;
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

    private int dest => Destination!.Address ?? throw new NullReferenceException($"Address is not set for the register '{Destination}'");
    private int src1 => Source1!.Address ?? throw new NullReferenceException($"Address is not set for the register '{Source1}'");
    private int src2 => Source2!.Address ?? throw new NullReferenceException($"Address is not set for the register '{Source2}'");
    private int immediateValue => ImmediateValue ?? throw new NullReferenceException("ImmediateValue is not set");
    private int targetAddress => JumpLabel!.Target?.StartInstructionAddress ?? throw new NullReferenceException("Target is not set for the label");
    private int argCount => ArgCount ?? throw new NullReferenceException($"ArgCount is not set");

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

    public Instruction Compile(int instructionAddress)
    {
        switch (Opcode)
        {
            case var reg2Reg when reg2Reg.IsRegisterToRegister:
                checked
                {
                    return new(reg2Reg, (byte)dest, (byte)src1, (byte)src2);
                }

            case Opcode.Brc:
                checked
                {
                    short jumpValue = (short)(targetAddress - instructionAddress);
                    return new(Opcode.Brc, 0, jumpValue);
                }

            case var branch and (Opcode.BrFl or Opcode.BrTr):
                checked
                {
                    short jumpValue = (short)(targetAddress - instructionAddress);
                    return new(branch, (byte)dest, jumpValue);
                }

            case Opcode.Move:
                checked
                {
                    return new(Opcode.Move, (byte)dest, (byte)src1, 0);
                }

            case Opcode.RetC:
                checked
                {
                    return new(Opcode.RetC, 0, (short)immediateValue);
                }

            case Opcode.Ret:
                checked
                {
                    return new(Opcode.Ret, 0, (byte)dest);
                }

            case var load and (Opcode.LdConst or Opcode.LdVar):
                checked
                {
                    return new(load, (byte)dest, (short)immediateValue);
                }

            case Opcode.Call:
                checked
                {
                    return new(Opcode.Call, (byte)dest, (byte)src1, (byte)argCount);
                }

            default:
                throw new UnreachableException();
        }
    }

    public override string ToString() => $"{Opcode}\trd({Destination}) rs1({Source1}) rs2({Source2}) imm({ImmediateValue}) argc({ArgCount})";
}
