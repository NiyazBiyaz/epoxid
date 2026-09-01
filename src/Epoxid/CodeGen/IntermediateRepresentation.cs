using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.CodeGen;

internal class Register
{
    public int StoredAddress { get; set; } = -1;
}

internal record Constant(EpObject Value)
{
    public int ImmediateValue { get; set; } = -1;
}

internal record Variable(string Name)
{
    public int ImmediateValue { get; set; } = -1;
}

internal class Label
{
    public IntermediateInstruction? InstructionOnLabel { get; set; }
}

internal record IntermediateLoop(Label HeadLabel, Label EndLabel);

internal record IntermediateInstruction(Opcode Opcode)
{
    public int InstructionAddress { get; set; }

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
}
