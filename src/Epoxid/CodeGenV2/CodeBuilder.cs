using Epoxid.Runtime;
using Epoxid.Runtime.Objects;
using Epoxid.VM;

namespace Epoxid.CodeGenV2;

internal class CodeBuilder
{
    private readonly List<Register> registers = [];
    private readonly List<EpObject> constants = [];
    private readonly List<string> freeVariables = [];

    private readonly List<ControlFlowBlock> cfgBlocks = [];

    private ControlFlowBlock currentBlock = new();

    public Label PutLabel(Label label)
    {
        if (currentBlock.Instructions.Count != 0)
            endCfgBlock();

        label.Target = currentBlock;

        return label;
    }

    public Register AllocateRegister()
    {
        var reg = new Register();
        registers.Add(reg);
        return reg;
    }

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

    private void addInstruction(IntermediateInstruction instruction)
    {
        currentBlock.Instructions.Add(instruction);

        if (instruction.Opcode.IsEndOfCfgBlock)
            endCfgBlock();
    }

    private void endCfgBlock()
    {
        cfgBlocks.Add(currentBlock);
        currentBlock = new();
    }

    #region Opcodes

    public void RegisterToRegister(Opcode opcode, Register dest, Register src1, Register src2)
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
    }

    public void LdVar(Register dest, int varIndex)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.LdVar,
            Destination = dest,
            ImmediateValue = varIndex,
        };
        addInstruction(instr);
    }

    public void LdConst(Register dest, int varIndex)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.LdConst,
            Destination = dest,
            ImmediateValue = varIndex,
        };
        addInstruction(instr);
    }

    public void Call(Register func, Register dest, int argCount)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.Call,
            Source1 = func,
            Destination = dest,
            ArgCount = argCount,
        };
        addInstruction(instr);
    }

    public void Move(Register dest, Register source)
    {
        var instr = new IntermediateInstruction
        {
            Opcode = Opcode.Move,
            Destination = dest,
            Source1 = source,
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
