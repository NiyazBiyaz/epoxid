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

    public int Address { get; set; }

    private int dest => Destination!.Address ?? throw new NullReferenceException($"Address is not set for the register '{Destination}'");
    private int src1 => Source1!.Address ?? throw new NullReferenceException($"Address is not set for the register '{Source1}'");
    private int src2 => Source2!.Address ?? throw new NullReferenceException($"Address is not set for the register '{Source2}'");
    private int immediateValue => ImmediateValue ?? throw new NullReferenceException("ImmediateValue is not set");
    private int targetAddress => JumpLabel?.Target?.FirstInstruction.Address ?? throw new NullReferenceException("Target is not set for the label");
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

    public Instruction Compile()
    {
        checked
        {
            return Opcode switch
            {
                var reg2Reg when reg2Reg.IsRegisterToRegister => new(reg2Reg, (byte)dest, (byte)src1, (byte)src2),

                var inPlace when inPlace.IsInPlace => new(inPlace, (byte)dest, (byte)src1, 0),

                Opcode.Brc => new(Opcode.Brc, 0, (short)(targetAddress - Address)),

                var branch and (Opcode.BrFl or Opcode.BrTr) => new(branch, (byte)dest, (short)(targetAddress - Address)),

                Opcode.Move => new(Opcode.Move, (byte)dest, (byte)src1, 0),

                Opcode.RetC => new(Opcode.RetC, 0, (short)immediateValue),

                Opcode.Ret => new(Opcode.Ret, (byte)dest, 0),

                var load and (Opcode.LdConst or Opcode.LdVar or Opcode.LdArg) => new(load, (byte)dest, (short)immediateValue),

                var store and Opcode.StVar => new(store, (byte)dest, (short)immediateValue),

                Opcode.Call => new(Opcode.Call, (byte)dest, (byte)src1, (byte)argCount),

                Opcode.BindFun => new(Opcode.BindFun, (byte)dest, 0),

                _ => throw new UnreachableException(),
            };
        }
    }

    public override string ToString() => $"{Opcode}\trd({Destination}) rs1({Source1}) rs2({Source2}) imm({ImmediateValue}) argc({ArgCount})";
}
