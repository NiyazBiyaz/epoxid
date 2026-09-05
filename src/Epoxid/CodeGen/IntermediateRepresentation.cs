using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.CodeGen;

internal class Register
{
    public int StoredAddress { get; set; } = -1;

    public IntermediateInstruction LastUsedInstruction { get; set; } = null!;

    public int Id { get; set; } = -1;

    public override string ToString() => $"R{Id}";
}

internal record Constant(EpObject Value)
{
    public int ImmediateValue { get; set; } = -1;

    public override string ToString() => ImmediateValue.ToString();
}

internal record Variable(string Name)
{
    public int ImmediateValue { get; set; } = -1;

    public override string ToString() => ImmediateValue.ToString();
}

internal class Label : IEquatable<Label>
{
    public IntermediateInstruction? InstructionOnLabel { get; set; }

    public InstructionId Id { get; set; } = null!;

    public bool Equals(Label? other)
    {
        if (other == null)
            return false;

        return other.Id == Id;
    }

    public override string ToString() => Id.ToString();
}

internal record IntermediateLoop(Label HeadLabel, Label EndLabel);

internal record IntermediateInstruction(Opcode Opcode)
{
    public int InstructionAddress { get; set; }

    public InstructionId Id { get; set; } = null!;

    public ControlFlowBlock? FlowBlock { get; set; }

    public Register? Dest { get; init; }
    public byte DestValue
    {
        get
        {
            if (Dest == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((byte)Dest.StoredAddress);
        }
    }

    public Register? Src1 { get; init; }
    public byte Src1Value
    {
        get
        {
            if (Src1 == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((byte)Src1.StoredAddress);
        }
    }

    public Register? Src2 { get; init; }
    public byte Src2Value
    {
        get
        {
            if (Src2 == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((byte)Src2.StoredAddress);
        }
    }

    public Constant? Constant { get; init; }
    public short ConstantValue
    {
        get
        {
            if (Constant == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((short)Constant.ImmediateValue);
        }
    }

    public Variable? Variable { get; init; }
    public short VariableValue
    {
        get
        {
            if (Variable == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((short)Variable.ImmediateValue);
        }
    }

    public Label? Label { get; init; }
    public short LabelValue
    {
        get
        {
            if (Label == null)
                throw new InvalidOperationException("Value was not set before");

            int index = Label.InstructionOnLabel?.InstructionAddress ?? throw new InvalidOperationException("Label does not have any attached instruction");

            return checked((short)(index - InstructionAddress));
        }
    }

    public int ArgCount { get; init; } = -1;
    public byte ArgCountValue => ArgCount != -1 ? checked((byte)ArgCount) : throw new InvalidOperationException("Value was not set before");

    public Instruction Lower() => checked(Opcode switch
    {
        _ when Opcode.IsRegisterToRegister => new Instruction(Opcode, DestValue, Src1Value, Src2Value),

        Opcode.LdConst => new Instruction(Opcode, DestValue, ConstantValue),

        Opcode.LdVar => new Instruction(Opcode, DestValue, VariableValue),

        Opcode.Ret => new Instruction(Opcode, 0, Src1Value, 0),

        Opcode.RetC => new Instruction(Opcode, 0, ConstantValue),

        Opcode.Call => new Instruction(Opcode, DestValue, Src1Value, ArgCountValue),

        Opcode.Move => new Instruction(Opcode, DestValue, Src1Value, 0),

        Opcode.BrTr or Opcode.BrFl => new Instruction(Opcode, DestValue, LabelValue),

        Opcode.Brc => new Instruction(Opcode, 0, LabelValue),

        _ => throw new NotImplementedException(),
    });

    public override string ToString() => Id.ToString() + ':' + '\t' + Opcode switch
    {
        _ when Opcode.IsRegisterToRegister => $"{Opcode}\t{Dest} {Src1} {Src2}",

        Opcode.LdConst => $"LdConst\t{Dest} {Constant}",

        Opcode.LdVar => $"LdVar\t{Dest} {Variable}",

        Opcode.Ret => $"Ret\t{Src1}",

        Opcode.RetC => $"RetC\t{Constant}",

        Opcode.Call => $"Call\t{Src1} {Dest} {Src2} {ArgCount}",

        Opcode.Move => $"Move\t{Dest} {Src1}",

        Opcode.BrTr or Opcode.BrFl => $"{Opcode}\t{Dest} {Label}",

        Opcode.Brc => $"Brc\t{Label}",

        _ => throw new NotImplementedException(),
    };
}

internal class ControlFlowBlock
{
    public required Label? StartLabel { get; set; }
    public required Memory<IntermediateInstruction> Instructions { get; set; }
    public required IntermediateInstruction EndInstruction { get; set; }

    public ControlFlowBlock? Next { get; set; }
}

/// <summary>
/// Reference class that represents ID of the instruction that label spot on.
/// Allows to compare different labels by value while they still reference types.
/// </summary>
internal record InstructionId(int Mnemonics)
{
    public override string ToString() => $"I{Mnemonics}";
}
