using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.CodeGen;

/// <summary>
/// Class to manage indexes of variable names, constant values, registers in the final instruction.
/// </summary>
internal class CodeBuilder
{
    private readonly List<IntermediateInstruction> instructions = [];
    private readonly List<Register> allocatedRegisters = [];
    private readonly List<Constant> constants = [];
    private readonly List<Variable> closureVariables = [];

    private readonly Stack<Label> labels = [];

    public CodeObject Dump()
    {
        if (labels.Count != 0)
            throw new InvalidOperationException("Cannot dump code object: builder have unresolved labels");

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

    public void PutLabel(Label newLabel) => labels.Push(newLabel);

    private void addInstruction(IntermediateInstruction instruction)
    {
        while (labels.TryPop(out var label))
        {
            if (label.InstructionOnLabel != null)
            {
                throw new InvalidOperationException("Label already has attached instruction");
            }

            label.InstructionOnLabel = instruction;
        }

        instructions.Add(instruction);
    }

    private Register allocateRegister()
    {
        var reg = new Register();
        allocatedRegisters.Add(reg);
        return reg;
    }

    private Constant addConstant(EpObject value)
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
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction LdVar(string name)
    {
        var instr = new IntermediateInstruction(Opcode.LdVar)
        {
            Dest = allocateRegister(),
            Variable = addVariable(name),
        };
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction LdConst(EpObject value)
    {
        var instr = new IntermediateInstruction(Opcode.LdConst)
        {
            Dest = allocateRegister(),
            Constant = addConstant(value),
        };
        addInstruction(instr);

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
        addInstruction(instr);

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
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction Move(Register source)
    {
        var instr = new IntermediateInstruction(Opcode.Move)
        {
            Dest = allocateRegister(),
            Src1 = source,
        };
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction Move(Register source, Register dest)
    {
        var instr = new IntermediateInstruction(Opcode.Move)
        {
            Dest = dest,
            Src1 = source,
        };
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction Ret(Register register)
    {
        var instr = new IntermediateInstruction(Opcode.Ret)
        {
            Src1 = register,
        };
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction RetC(EpObject constantValue)
    {
        var instr = new IntermediateInstruction(Opcode.RetC)
        {
            Constant = addConstant(constantValue),
        };
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction Brc(Label target)
    {
        var instr = new IntermediateInstruction(Opcode.Brc)
        {
            Label = target,
        };
        addInstruction(instr);

        return instr;
    }

    public IntermediateInstruction BrTr(Label target, Register condition)
    {
        var instr = new IntermediateInstruction(Opcode.BrTr)
        {
            Label = target,
            Dest = condition,
        };
        addInstruction(instr);

        return instr;
    }
    public IntermediateInstruction BrFl(Label target, Register condition)
    {
        var instr = new IntermediateInstruction(Opcode.BrFl)
        {
            Label = target,
            Dest = condition,
        };
        addInstruction(instr);

        return instr;
    }
}
