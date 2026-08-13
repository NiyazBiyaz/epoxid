using PySharp.Runtime.Objects;
using PySharp.VM;

namespace PySharp.SyntaxAnalysis;

/// <summary>
/// Class to manage indexes of variable names, constant values, registers in the final instruction.
/// </summary>
internal class CodeBuilder
{
    private readonly List<IntermediateInstruction> instructions = [];
    private readonly List<Register> allocatedRegisters = [];
    private readonly List<Constant> constants = [];
    private readonly List<Variable> closureVariables = [];

    public CodeObject Dump()
    {
        resolveIndexes();

        return new CodeObject
        {
            Instructions = [.. instructions.Select(irI => irI.Lower())],
            Constants = [.. constants.Select(c => c.Value)],
            VarNames = [.. closureVariables.Select(v => v.Name)],
            StackSize = allocatedRegisters.Count,
        };
    }

    private void resolveIndexes()
    {
        foreach (var (i, instr) in instructions.Index())
        {
            instr.Index = i;
        }

        foreach (var (i, reg) in allocatedRegisters.Index())
        {
            reg.Index = i;
        }

        foreach (var (i, constant) in constants.Index())
        {
            constant.Index = i;
        }

        foreach (var (i, variable) in closureVariables.Index())
        {
            variable.Index = i;
        }
    }

    private Register allocateRegister()
    {
        var reg = new Register();
        allocatedRegisters.Add(reg);
        return reg;
    }

    private Constant addConstant(PsObject value)
    {
        if (constants.FirstOrDefault(con => con.Value == value) is Constant existingConstant)
            return existingConstant;

        var constant = new Constant(value);
        constants.Add(constant);
        return constant;
    }

    private Variable addVariable(string name)
    {
        if (closureVariables.FirstOrDefault(var => var.Name == name) is Variable existingVariable)
            return existingVariable;

        var variable = new Variable(name);
        closureVariables.Add(variable);
        return variable;
    }

    public IntermediateInstruction RegisterToRegister(Opcode opcode, Register src1, Register src2)
    {
        if (!opcode.IsRegisterToRegister)
            throw new ArgumentOutOfRangeException(nameof(opcode), "Invalid opcode value for this instruction format");

        var instr = new IntermediateInstruction(opcode)
        {
            Dest = allocateRegister(),
            Src1 = src1,
            Src2 = src2,
        };

        instructions.Add(instr);

        return instr;
    }

    public IntermediateInstruction LdVar(string name)
    {
        var instr = new IntermediateInstruction(Opcode.LdVar)
        {
            Dest = allocateRegister(),
            VarName = addVariable(name),
        };

        instructions.Add(instr);

        return instr;
    }

    public IntermediateInstruction LdConst(PsObject value)
    {
        var instr = new IntermediateInstruction(Opcode.LdConst)
        {
            Dest = allocateRegister(),
            Constant = addConstant(value),
        };

        instructions.Add(instr);

        return instr;
    }

    public IntermediateInstruction Call(Register function, Register destination, int argCount)
    {
        var instr = new IntermediateInstruction(Opcode.Call)
        {
            Dest = destination,
            Src1 = function,
            ArgCount = argCount,
        };

        instructions.Add(instr);

        return instr;
    }

    public IntermediateInstruction CallK(Register function, Register destination, int argCount)
    {
        var instr = new IntermediateInstruction(Opcode.CallK)
        {
            Dest = destination,
            Src1 = function,
            ArgCount = argCount,
        };

        instructions.Add(instr);

        return instr;
    }

    public IntermediateInstruction Move(Register source)
    {
        var instr = new IntermediateInstruction(Opcode.Move)
        {
            Dest = allocateRegister(),
            Src1 = source,
        };
        instructions.Add(instr);

        return instr;
    }

    public IntermediateInstruction Ret(Register register)
    {
        var instr = new IntermediateInstruction(Opcode.Ret)
        {
            Src1 = register,
        };

        instructions.Add(instr);

        return instr;
    }
}

internal class Register
{
    public int Index { get; set; }
}

internal record Constant(PsObject Value)
{
    public int Index { get; set; }
}

internal record Variable(string Name)
{
    public int Index { get; set; }
}

internal record IntermediateInstruction(Opcode Opcode)
{
    public int Index { get; set; }

    public Register? Dest { get; init; }
    public byte DestValue => Dest != null ? checked((byte)Dest.Index) : throw new InvalidOperationException("Value was not set before");

    public Register? Src1 { get; init; }
    public byte Src1Value => Src1 != null ? checked((byte)Src1.Index) : throw new InvalidOperationException("Value was not set before");

    public Register? Src2 { get; init; }
    public byte Src2Value => Src2 != null ? checked((byte)Src2.Index) : throw new InvalidOperationException("Value was not set before");

    public Constant? Constant { get; init; }
    public short ConstantValue => Constant != null ? checked((short)Constant.Index) : throw new InvalidOperationException("Value was not set before");

    public Variable? VarName { get; init; }
    public short VarNameValue => VarName != null ? checked((short)VarName.Index) : throw new InvalidOperationException("Value was not set before");

    public IntermediateInstruction? JumpDest { get; init; }
    public short JumpDestValue => JumpDest != null ? checked((short)JumpDest.Index) : throw new InvalidOperationException("Value was not set before");

    public int ArgCount { get; init; } = -1;
    public byte ArgCountValue => ArgCount != -1 ? checked((byte)ArgCount) : throw new InvalidOperationException("Value was not set before");

    public Instruction Lower() => checked(Opcode switch
    {
        _ when Opcode.IsRegisterToRegister => new Instruction(Opcode, DestValue, Src1Value, Src2Value),

        Opcode.LdConst => new Instruction(Opcode, DestValue, ConstantValue),

        Opcode.LdVar => new Instruction(Opcode, DestValue, VarNameValue),

        Opcode.Ret => new Instruction(Opcode, 0, Src1Value, 0),

        Opcode.RetC => new Instruction(Opcode, 0, ConstantValue),

        Opcode.Call or Opcode.CallK => new Instruction(Opcode, DestValue, Src1Value, ArgCountValue),

        Opcode.Move => new Instruction(Opcode, DestValue, Src1Value, 0),

        _ => throw new NotImplementedException(),
    });
}
