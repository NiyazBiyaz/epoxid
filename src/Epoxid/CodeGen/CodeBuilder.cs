using System.Collections.Immutable;
using System.Diagnostics;
using Epoxid.Runtime;
using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.CodeGen;

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
    internal IReadOnlyList<EpObject> Constants => constants;

    private readonly List<string> freeVariables = [];

    private int virtualRegistersCount = 0;

    private int callCount = 0;

    private readonly List<ControlFlowBlock> cfgBlocks = [];

    private ControlFlowBlock currentBlock = new(0);

    private int stackSize = 0;

    private bool pendingLoopLifetime = false;

    private readonly HashSet<Register> loopLifetimeRegisters = [];
    private ControlFlowBlock? loopLifetimeStart;

    public bool CanBeCompleted { get; private set; }

    public const int NoneConstantIndex = 0;
    public const int TrueConstantIndex = 1;
    public const int FalseConstantIndex = 2;
    public const int EllipsisConstantIndex = 3;

    public Label PutLabel(Label label)
    {
        if (currentBlock.Instructions.Count != 0)
            endCfgBlock();

        if (pendingLoopLifetime)
        {
            pendingLoopLifetime = false;
            loopLifetimeStart = currentBlock;
        }

        label.Target = currentBlock;
        currentBlock.AddReferredLabel(label);

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

    public void BeginLoopLifetime()
    {
        loopLifetimeRegisters.Clear();
        pendingLoopLifetime = true;
    }

    public void EndLoopLifetime()
    {
        Debug.Assert(loopLifetimeStart != null);

        foreach (var register in loopLifetimeRegisters)
        {
            register.BlocksUsages.Add(loopLifetimeStart, UsageSpan.Full);
            register.BlocksUsages.Add(currentBlock, UsageSpan.Full);
        }
    }

    public void AddToLoopLifetime(Register register) => loopLifetimeRegisters.Add(register);

    public int BeginNewCall() => callCount++;

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

    public CodeObject Compile()
    {
        resolveRegisterAddresses();

        List<InstructionRepr> instructions1 = cfgBlocks
            .SelectMany(b => b.Instructions.Select(i => new InstructionRepr(i, b.Id)))
            .ToList();

        var instructions2 = new List<InstructionRepr>();

        optimize(instructions1, instructions2, getOptimizedFirstPass);
        instructions1.Clear();
        optimize(instructions2, instructions1, getOptimizedSecondPass);

        var optimized = instructions1;

        // Optimizer doesn't patches back cfg blocks, so if block was deleted completely,
        // branch that was referred to this block wouldn't be able to locate jump label
        // correctly. So this is why this address marking that strange.
        int previousBlockId = 0;
        for (int instructionNumber = 0; instructionNumber < optimized.Count; instructionNumber++)
        {
            var repr = optimized[instructionNumber];
            repr.Instruction.Address = instructionNumber;

            while (previousBlockId < repr.BlockId)
            {
                cfgBlocks[++previousBlockId].FirstInstruction.Address = instructionNumber;
            }
        }

        var instructions = ImmutableArray.CreateBuilder<Instruction>(initialCapacity: optimized.Count);

        foreach (var repr in optimized)
        {
            instructions.Add(repr.Instruction.Compile());
        }

        return new()
        {
            Constants = [.. constants],
            VarNames = [.. freeVariables],
            StackSize = stackSize,
            Instructions = instructions.ToImmutable(),
        };
    }

    private readonly record struct InstructionRepr(IntermediateInstruction Instruction, int BlockId);

    private static void optimize(List<InstructionRepr> source, List<InstructionRepr> dest, Func<InstructionRepr, InstructionRepr?, OptimizerResult> pass)
    {
        for (int index = 0; index < source.Count; index++)
        {
            var instr = source[index];

            OptimizerResult optimized;
            if (index + 1 < source.Count)
            {
                optimized = pass(instr, source[index + 1]);
            }
            else
            {
                optimized = pass(instr, null);
            }

            switch (optimized)
            {
                case OptimizerResult.Keep:
                    dest.Add(instr);
                    break;
                case OptimizerResult.Remove:
                    break;
                case OptimizerResult.RemoveBoth:
                    index += 1;
                    break;
            }
        }
    }

    private static OptimizerResult getOptimizedFirstPass(InstructionRepr instr, InstructionRepr? next)
        => instr.Instruction switch
        {
            {
                Opcode: Opcode.Move,
                Source1.Address: int src1,
                Destination.Address: int dest,
            } when src1 == dest => OptimizerResult.Remove,
            {
                Opcode: Opcode.LdConst,
                ImmediateValue: NoneConstantIndex,
                Destination.CallId: not null,
            } => OptimizerResult.Remove,
            {
                Opcode: Opcode.Brc or Opcode.BrTr or Opcode.BrFl,
                JumpLabel.Target.Id: var targetId,
            } when targetId == next?.BlockId => OptimizerResult.Remove,

            _ => OptimizerResult.Keep,
        };

    private static OptimizerResult getOptimizedSecondPass(InstructionRepr instr, InstructionRepr? next)
        => (instr, next) switch
        {
            {
                instr.Instruction:
                {
                    Opcode: Opcode.Move,
                    Destination.Address: int dest,
                    Source1.Address: int src1,
                },
                next.Instruction:
                {
                    Opcode: Opcode.Move,
                    Destination.Address: int nextDest,
                    Source1.Address: int nextSrc1,
                }
            } when dest == nextSrc1 && src1 == nextDest => OptimizerResult.RemoveBoth,

            _ => OptimizerResult.Keep,
        };

    private enum OptimizerResult
    {
        Keep,
        Remove,
        RemoveBoth,
    }

    private void addInstruction(IntermediateInstruction instruction)
    {
        currentBlock.Instructions.Add(instruction);
        CanBeCompleted = false;

        if (instruction.Opcode.IsEndOfCfgBlock)
        {
            endCfgBlock();
            CanBeCompleted = true;
        }
    }

    private void resolveRegisterAddresses()
    {
        var firstBlock = cfgBlocks[0];

        var visited = new HashSet<ControlFlowBlock>();
        var pendingBranchReferenceCounts = cfgBlocks
            .Select(static block => (block, block.BranchedReferenceCount))
            .ToDictionary();

        void markupRegistersOfBlock(ControlFlowBlock block, IEnumerable<int> level)
        {
            visited.Add(block);

            block.BranchLevel = [.. level, block.Id];

            for (int instructionNumber = 0; instructionNumber < block.Instructions.Count; instructionNumber++)
            {
                var instruction = block.Instructions[instructionNumber];

                safelyAddUsage(block, instructionNumber, instruction.Destination);
                safelyAddUsage(block, instructionNumber, instruction.Source1);
                safelyAddUsage(block, instructionNumber, instruction.Source2);

                if (instruction.ArgCount != null)
                {
                    var callRegisters = registers.Where(r => r.CallId == instruction.Destination!.CallId);

                    foreach (var reg in callRegisters)
                    {
                        // First two registers of call instruction already set as used
                        if (reg.CallRelativeAddress < 2)
                            continue;

                        safelyAddUsage(block, instructionNumber, reg);
                    }
                }

                static void safelyAddUsage(ControlFlowBlock block, int instructionNumber, Register? register)
                {
                    if (register == null)
                        return;

                    if (!register.BlocksUsages.TryGetValue(block, out var usage))
                    {
                        usage = register.BlocksUsages[block] = new();
                    }

                    usage.AddUsagePoint(instructionNumber);
                }
            }

            if (block.Branched is ControlFlowBlock branchedBlock)
            {
                if (block.Branched != block.Next)
                    level = level.Append(block.Id);

                if (!visited.Contains(branchedBlock))
                {
                    if (block.LastInstruction.Opcode == Opcode.Brc)
                        level = level.Take(level.Count() - branchedBlock.BranchedReferenceCount);

                    if (--pendingBranchReferenceCounts[branchedBlock] == 0)
                        markupRegistersOfBlock(branchedBlock, level);
                }
            }
            if (block.Next is ControlFlowBlock nextBlock)
            {
                if (pendingBranchReferenceCounts[nextBlock] == 0 && !visited.Contains(nextBlock))
                {
                    level = level.Take(level.Count() - nextBlock.BranchedReferenceCount);
                    markupRegistersOfBlock(nextBlock, level);
                }
            }
        }

        markupRegistersOfBlock(firstBlock, []);

        for (int leftIndex = 0; leftIndex < registers.Count; leftIndex++)
        {
            var thisRegister = registers[leftIndex];
            for (int rightIndex = leftIndex + 1; rightIndex < registers.Count; rightIndex++)
            {
                var otherRegister = registers[rightIndex];

                if (thisRegister.CollidesWith(otherRegister))
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

    private void endCfgBlock()
    {
        cfgBlocks.Add(currentBlock);

        if (currentBlock.LastInstruction.Opcode is Opcode.BrFl or Opcode.BrTr or Opcode.Brc)
        {
            currentBlock.SetBranchedLabel(currentBlock.LastInstruction.JumpLabel ?? throw new InvalidOperationException());
        }

        var next = new ControlFlowBlock(cfgBlocks.Count);

        if (currentBlock.LastInstruction.Opcode is not (Opcode.Ret or Opcode.RetC or Opcode.Brc))
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

    public Register InAdd(Register dest, Register src1)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.InAdd,
            Destination = dest,
            Source1 = src1,
        };
        addInstruction(instr);

        return dest;
    }

    public Register InSub(Register dest, Register src1)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.InSub,
            Destination = dest,
            Source1 = src1,
        };
        addInstruction(instr);

        return dest;
    }

    public Register InMul(Register dest, Register src1)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.InMul,
            Destination = dest,
            Source1 = src1,
        };
        addInstruction(instr);

        return dest;
    }

    public Register InTDiv(Register dest, Register src1)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.InTDiv,
            Destination = dest,
            Source1 = src1,
        };
        addInstruction(instr);

        return dest;
    }

    public Register InMod(Register dest, Register src1)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.InMod,
            Destination = dest,
            Source1 = src1,
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

    public void StVar(Register source, int varIndex)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.StVar,
            Destination = source,
            ImmediateValue = varIndex,
        };
        addInstruction(instr);
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

    public Register LdArg(Register dest, int paramIndex)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.LdArg,
            Destination = dest,
            ImmediateValue = paramIndex,
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

    public void Ret(Register returnValue)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.Ret,
            Destination = returnValue,
        };
        addInstruction(instr);
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

    public void Brc(Label label, bool backwardJump = false)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.Brc,
            JumpLabel = label,
        };
        addInstruction(instr);

        if (!backwardJump)
            label.BranchUsageCount++;
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

        label.BranchUsageCount++;
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

        label.BranchUsageCount++;
    }

    public void BindFun(Register functionRegister)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.BindFun,
            Destination = functionRegister,
        };
        addInstruction(instr);
    }

    #endregion
}
