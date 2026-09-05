using System.Text;
using Epoxid.Runtime;
using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.CodeGen;

/// <summary>
/// Class to manage indexes of variable names, constant values, registers in the final instruction.
/// </summary>
internal class CodeBuilder
{
    private readonly List<IntermediateInstruction> instructions = [];
    private readonly List<Register> registers = [];
    private readonly List<Constant> constants = [];
    private readonly List<Variable> freeVariables = [];

    private readonly Dictionary<InstructionId, ControlFlowBlock> controlFlowGraph = [];

    private readonly Stack<Label> pendingLabels = [];
    private readonly Dictionary<InstructionId, Label> labels = [];

    public CodeObject Dump()
    {
        if (pendingLabels.Count != 0)
            throw new InvalidOperationException("Cannot dump code object: builder have unresolved labels");

        resolveIndexes();

        return new CodeObject
        {
            Instructions = [.. instructions.Select(irI => irI.Lower())],
            Constants = [.. constants.Select(c => c.Value)],
            VarNames = [.. freeVariables.Select(v => v.Name)],
            StackSize = registers.Count,
        };
    }

    public void PutLabel(Label newLabel) => pendingLabels.Push(newLabel);

    public void CreateControlFlowGraph()
    {
        for (int index = 0; index < instructions.Count; index++)
        {
            var instr = instructions[index];
            int blockEnd = instructions.FindIndex(index, instr => instr.Opcode.IsEndOfCfgBlock);
            controlFlowGraph[instr.Id] = instr.FlowBlock = new ControlFlowBlock
            {
                StartLabel = labels.TryGetValue(instr.Id, out var label) ? label : null,
                Instructions = instructions[index..blockEnd].ToArray(),
                EndInstruction = instructions[blockEnd],
            };
            index = blockEnd;
        }

        Console.WriteLine(string.Join(", ", controlFlowGraph));

        foreach (var block in controlFlowGraph.Values)
        {
            if (block.EndInstruction.Opcode.IsBranch)
            {
                block.Next = controlFlowGraph[block.EndInstruction.Label!.Id];
            }
        }
    }

    public string DumpCfg()
    {
        var sb = new StringBuilder();

        bool addLine = false;

        foreach (var block in controlFlowGraph.Values)
        {
            if (addLine)
                sb.Append("---------------------------\n");
            addLine = true;

            foreach (var instr in block.Instructions.Span)
            {
                sb.Append(instr.ToString());
                sb.Append('\n');
            }

            sb.Append(block.EndInstruction.ToString());
            sb.Append('\n');
        }

        return sb.ToString();
    }

    private void resolveIndexes()
    {
        foreach (var (i, instr) in instructions.Index())
        {
            instr.InstructionAddress = i;
        }

        foreach (var (i, reg) in registers.Index())
        {
            reg.StoredAddress = i;
        }
    }

    private void addInstruction(IntermediateInstruction instruction)
    {
        instruction.Id = new(instructions.Count);

        bool addToLabels = true;

        while (pendingLabels.TryPop(out var label))
        {
            if (label.InstructionOnLabel != null)
            {
                throw new InvalidOperationException("Label already has attached instruction");
            }

            label.InstructionOnLabel = instruction;
            label.Id = instruction.Id;

            if (addToLabels)
            {
                labels[instruction.Id] = label;
                addToLabels = false;
            }
        }

        instructions.Add(instruction);

        instruction.Dest?.LastUsedInstruction = instruction;
        instruction.Src1?.LastUsedInstruction = instruction;
        instruction.Src2?.LastUsedInstruction = instruction;
    }

    private Register allocateRegister()
    {
        var reg = new Register
        {
            Id = registers.Count,
        };
        registers.Add(reg);
        return reg;
    }

    private Constant addConstant(EpObject value)
    {
        if (constants.FirstOrDefault(constant => byValueEpObjectEquals(constant.Value)) is Constant existingConstant)
            return existingConstant;

        var constant = new Constant(value)
        {
            ImmediateValue = constants.Count,
        };
        constants.Add(constant);
        return constant;

        bool byValueEpObjectEquals(EpObject constant)
        {
            if (constant.DunderClass != value.DunderClass)
                return false;

            var equality = Core.EqualObjects(constant, value);
            return Core.ConvertToBool(equality);
        }
    }

    private Variable addVariable(string name)
    {
        if (freeVariables.FirstOrDefault(var => var.Name == name) is Variable existingVariable)
            return existingVariable;

        var variable = new Variable(name)
        {
            ImmediateValue = freeVariables.Count,
        };
        freeVariables.Add(variable);
        return variable;
    }

    #region Opcodes

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

    #endregion
}
