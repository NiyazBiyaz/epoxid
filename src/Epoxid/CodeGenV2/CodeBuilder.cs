using Epoxid.Runtime;
using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.CodeGenV2;

internal class CodeBuilder
{
    private readonly List<Register> registers = [];
    private readonly List<EpObject> constants =
    [
        EpConstants.None,
        EpConstants.True,
        EpConstants.False,
        EpConstants.Ellipsis,
    ];
    private readonly List<string> freeVariables = [];

    private int virtualRegistersCount = 0;

    private int callCount = 0;

    public readonly List<ControlFlowBlock> CfgBlocks = [];

    private ControlFlowBlock currentBlock = new(0);

    private int stackSize = 0;

    public const int NoneConstantIndex = 0;
    public const int TrueConstantIndex = 1;
    public const int FalseConstantIndex = 2;
    public const int EllipsisConstantIndex = 3;

    public Label PutLabel(Label label)
    {
        if (currentBlock.Instructions.Count != 0)
            endCfgBlock();

        label.Target = currentBlock;
        currentBlock.LabelsCount++;

        return label;
    }

    public Register AllocateRegister()
    {
        var reg = new Register
        {
            Id = virtualRegistersCount++,
        };
        registers.Add(reg);
        return reg;
    }

    public Register AllocateRegister(int callId, int relativeAddress, int callSize)
    {
        var reg = new Register
        {
            Id = virtualRegistersCount++,
            CallId = callId,
            CallRelativeAddress = relativeAddress,
            CallCount = callSize,
        };
        registers.Add(reg);
        return reg;
    }

    public int StartCall() => callCount++;

    public int AddVariable(string variable)
    {
        int existing = freeVariables.FindIndex(fv => fv == variable);
        if (existing != -1)
        {
            return existing;
        }
        int newIndex = freeVariables.Count;
        freeVariables.Add(variable);
        return newIndex;
    }

    public int AddConstant(EpObject value)
    {
        int existing = constants.FindIndex(byValueEpObjectEquals);
        if (existing != -1)
        {
            return existing;
        }
        int index = constants.Count;
        constants.Add(value);
        return index;

        bool byValueEpObjectEquals(EpObject constant)
        {
            if (constant.DunderClass != value.DunderClass)
                return false;

            var equality = Core.EqualObjects(constant, value);
            return Core.ConvertToBool(equality);
        }
    }

    public void ResolveRegisterAddresses()
    {
        var singleBlock = CfgBlocks[0];

        for (int instructionNumber = 0; instructionNumber < singleBlock.Instructions.Count; instructionNumber++)
        {
            var instruction = singleBlock.Instructions[instructionNumber];

            instruction.Destination?.Usage.AddUsage(instructionNumber);
            instruction.Source1?.Usage.AddUsage(instructionNumber);
            instruction.Source2?.Usage.AddUsage(instructionNumber);

            if (instruction.ArgCount != null)
            {
                var callRegs = registers.Where(r => r.CallId == instruction.Destination!.CallId);

                foreach (var reg in callRegs)
                {
                    if (reg.CallRelativeAddress < 2)
                        continue;
                    reg.Usage.AddUsage(instructionNumber);
                }
            }
        }

        for (int leftIndex = 0; leftIndex < registers.Count; leftIndex++)
        {
            var thisRegister = registers[leftIndex];
            for (int rightIndex = leftIndex + 1; rightIndex < registers.Count; rightIndex++)
            {
                var otherRegister = registers[rightIndex];

                if (thisRegister.Usage.CollidesWith(otherRegister.Usage))
                {
                    thisRegister.LifetimeCollisions.Add(otherRegister);
                    otherRegister.LifetimeCollisions.Add(thisRegister);
                }
            }
        }

        // We have up to 256 registers, so this is absolutely fine to use greedy coloring.
        var descendingByCollisions = registers.OrderByDescending(r => r.ColoringSortScore);
        int stackSize = 0;

        Span<bool> neighborColors = stackalloc bool[256];
        foreach (var register in descendingByCollisions)
        {
            neighborColors.Clear();
            if (register.Address != null)
                continue;

            foreach (var neighbor in register.LifetimeCollisions)
            {
                if (neighbor.Address is not int neighborColor)
                    continue;

                neighborColors[neighborColor] = true;
            }

            if (register.CallId != null)
            {
                int addressBase = 0;
                for (; addressBase < 256; addressBase++)
                {
                    var neededRegisters = neighborColors.Slice(addressBase, register.CallCount!.Value);

                    if (!neededRegisters.Contains(true))
                        break;
                }

                foreach (var callRegister in registers.Where(r => r.CallId == register.CallId))
                {
                    int callRegAddress = (callRegister.CallRelativeAddress ?? throw new InvalidOperationException()) + addressBase;
                    callRegister.Address = callRegAddress;
                    stackSize = int.Max(stackSize, callRegAddress + 1);
                }
                continue;
            }

            int result = neighborColors.IndexOf(false);
            register.Address = result;
            stackSize = int.Max(stackSize, result + 1);
        }

        this.stackSize = stackSize;
    }

    public void Optimize()
    {
        var singleBlock = CfgBlocks[0];

        for (int index = 0; index < singleBlock.Instructions.Count; index++)
        {
            var instr = singleBlock.Instructions[index];
            if (instr.Opcode == Opcode.Move)
            {
                if (instr.Source1!.Address == instr.Destination!.Address)
                {
                    singleBlock.Instructions.RemoveAt(index);
                    index--;
                }
            }
            if (instr.Opcode == Opcode.LdConst)
            {
                if (instr.ImmediateValue == NoneConstantIndex && instr.Destination!.CallId != null)
                {
                    singleBlock.Instructions.RemoveAt(index);
                    index--;
                }
            }
        }
    }

    public void ResolveLabels()
    {
        int instructionCount = 0;
        foreach (var block in CfgBlocks)
        {
            block.StartInstructionAddress = instructionCount;
            instructionCount += block.Instructions.Count;
        }
    }

    public CodeObject Compile()
    {
        var singleBlock = CfgBlocks[0];

        var instructions = singleBlock
            .Instructions
            .Select((instr, relativeAddress) => instr.Compile(relativeAddress + singleBlock.StartInstructionAddress));

        return new()
        {
            Constants = [.. constants],
            VarNames = [.. freeVariables],
            StackSize = stackSize,
            Instructions = [.. instructions],
        };
    }

    private void addInstruction(IntermediateInstruction instruction)
    {
        currentBlock.Instructions.Add(instruction);

        if (instruction.Opcode.IsEndOfCfgBlock)
            endCfgBlock();
    }

    private void endCfgBlock()
    {
        CfgBlocks.Add(currentBlock);

        if (currentBlock.EndInstruction.Opcode is Opcode.BrFl or Opcode.BrTr)
        {
            currentBlock.SetBranchedLabel(currentBlock.EndInstruction.JumpLabel ?? throw new InvalidOperationException());
        }

        var next = new ControlFlowBlock(CfgBlocks.Count);

        if (currentBlock.EndInstruction.Opcode is not (Opcode.Ret or Opcode.RetC))
        {
            currentBlock.Next = next;
        }
        currentBlock = next;
    }

    #region Opcodes

    public Register RegisterToRegister(Opcode opcode, Register dest, Register src1, Register src2)
    {
        if (!opcode.IsRegisterToRegister)
            throw new ArgumentOutOfRangeException(nameof(opcode));

        var instr = new IntermediateInstruction
        {
            Opcode = opcode,
            Destination = dest,
            Source1 = src1,
            Source2 = src2,
        };
        addInstruction(instr);

        return dest;
    }

    public Register LdVar(Register dest, int varIndex)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.LdVar,
            Destination = dest,
            ImmediateValue = varIndex,
        };
        addInstruction(instr);

        return dest;
    }

    public Register LdConst(Register dest, int varIndex)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.LdConst,
            Destination = dest,
            ImmediateValue = varIndex,
        };
        addInstruction(instr);

        return dest;
    }

    public Register Call(Register func, Register dest, int argCount)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.Call,
            Source1 = func,
            Destination = dest,
            ArgCount = argCount,
        };
        addInstruction(instr);

        return dest;
    }

    public Register Move(Register dest, Register source)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.Move,
            Destination = dest,
            Source1 = source,
        };
        addInstruction(instr);

        return dest;
    }

    public void RetC(int constIndex)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.RetC,
            ImmediateValue = constIndex,
        };
        addInstruction(instr);
    }

    public void Brc(Label label)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.Brc,
            JumpLabel = label,
        };
        addInstruction(instr);
    }

    public void BrTr(Label label, Register condition)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.BrTr,
            JumpLabel = label,
            Destination = condition,
        };
        addInstruction(instr);
    }

    public void BrFl(Label label, Register condition)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.BrFl,
            JumpLabel = label,
            Destination = condition,
        };
        addInstruction(instr);
    }

    #endregion
}
