using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.SyntaxAnalysis;

internal class Register
{
    public int Index { get; set; }
}

internal record Constant(EpObject Value)
{
    public int Index { get; set; }
}

internal record Variable(string Name)
{
    public int Index { get; set; }
}

internal class Label
{
    public IntermediateInstruction? InstructionOnLabel { get; set; }
}

internal record IntermediateInstruction(Opcode Opcode)
{
    public int Index { get; set; }

    public Register? Dest { get; init; }
    public byte DestValue
    {
        get
        {
            if (Dest == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((byte)Dest.Index);
        }
    }

    public Register? Src1 { get; init; }
    public byte Src1Value
    {
        get
        {
            if (Src1 == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((byte)Src1.Index);
        }
    }

    public Register? Src2 { get; init; }
    public byte Src2Value
    {
        get
        {
            if (Src2 == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((byte)Src2.Index);
        }
    }

    public Constant? Constant { get; init; }
    public short ConstantValue
    {
        get
        {
            if (Constant == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((short)Constant.Index);
        }
    }

    public Variable? Variable { get; init; }
    public short VariableValue
    {
        get
        {
            if (Variable == null)
                throw new InvalidOperationException("Value was not set before");

            return checked((short)Variable.Index);
        }
    }

    public Label? Label { get; init; }
    public short LabelValue
    {
        get
        {
            if (Label == null)
                throw new InvalidOperationException("Value was not set before");

            int index = Label.InstructionOnLabel?.Index ?? throw new InvalidOperationException("Label does not have any attached instruction");

            return checked((short)(index - Index));
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

        Opcode.Call or Opcode.CallK => new Instruction(Opcode, DestValue, Src1Value, ArgCountValue),

        Opcode.Move => new Instruction(Opcode, DestValue, Src1Value, 0),

        Opcode.BrTr or Opcode.BrFl => new Instruction(Opcode, DestValue, LabelValue),

        Opcode.Brc => new Instruction(Opcode, LabelValue),

        _ => throw new NotImplementedException(),
    });
}

