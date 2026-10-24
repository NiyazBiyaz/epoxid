using Epoxid.Runtime.Objects;
using Epoxid.Runtime;
using Epoxid.VM;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Epoxid.SyntaxAnalysis.Common.Ast;

namespace Epoxid.CodeGen;

internal partial class CodeBuilder(SymbolTable symbolTable)
{
    private readonly List<IntermediateInstruction> instructions = [];

    private readonly Stack<Loop> loops = [];

    private readonly List<string> variableNames = [];

    private int stackDeltaAccumulator = 0;

    private readonly List<EpObject> constants =
    [
        EpNone.None,
        EpBool.True,
        EpBool.False,
        EpEllipsis.Ellipsis,
    ];

    public const int NoneConstantIndex = 0;
    public const int TrueConstantIndex = 1;
    public const int FalseConstantIndex = 2;
    public const int EllipsisConstantIndex = 3;

    public bool RequiresReturnNone => !instructions[^1].Opcode.IsCanBeLast;

    public bool IsFunction { get; init; } = false;

    public SymbolTable SymbolTable { get; } = symbolTable;

    public int AddVariable(string variable)
    {
        int existing = variableNames.FindIndex(fv => fv == variable);
        if (existing != -1)
        {
            return existing;
        }
        int newIndex = variableNames.Count;
        variableNames.Add(variable);
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

    public IntermediateInstruction CreateLabel() => new()
    {
        IsLabel = true,
    };

    public void PutLabel(IntermediateInstruction label)
    {
        Debug.Assert(label.IsLabel);

        addInstruction(label);
    }

    public void BeginLoop(Loop loop) => loops.Push(loop);

    public bool TryGetLoop([NotNullWhen(true)] out Loop? loop) => loops.TryPeek(out loop);

    public void EndLoop() => loops.Pop();

    public void ClearStack(IRedView view)
    {
        Debug.Assert(stackDeltaAccumulator > -1, stackDeltaAccumulator.ToString());

        if (stackDeltaAccumulator != 0)
        {
            Pop(stackDeltaAccumulator);
        }
        stackDeltaAccumulator = 0;
    }

    private void addInstruction(IntermediateInstruction instruction)
    {
        if (!instruction.IsLabel)
            stackDeltaAccumulator += instruction.StackDelta;
        instructions.Add(instruction);
    }

    #region Opcodes

    public void Add() => addInstruction(new()
    {
        Opcode = Opcode.Add,
    });

    public void Sub() => addInstruction(new()
    {
        Opcode = Opcode.Sub,
    });

    public void Mul() => addInstruction(new()
    {
        Opcode = Opcode.Mul,
    });

    public void TDiv() => addInstruction(new()
    {
        Opcode = Opcode.TDiv,
    });

    public void Mod() => addInstruction(new()
    {
        Opcode = Opcode.Mod,
    });

    public void Eq() => addInstruction(new()
    {
        Opcode = Opcode.Eq,
    });

    public void NEq() => addInstruction(new()
    {
        Opcode = Opcode.NEq,
    });

    public void GrTh() => addInstruction(new()
    {
        Opcode = Opcode.GrTh,
    });

    public void LsTh() => addInstruction(new()
    {
        Opcode = Opcode.LsTh,
    });

    public void InAdd() => addInstruction(new()
    {
        Opcode = Opcode.InAdd,
    });

    public void InSub() => addInstruction(new()
    {
        Opcode = Opcode.InSub,
    });

    public void InMul() => addInstruction(new()
    {
        Opcode = Opcode.InMul,
    });

    public void InTDiv() => addInstruction(new()
    {
        Opcode = Opcode.InTDiv,
    });

    public void InMod() => addInstruction(new()
    {
        Opcode = Opcode.InMod,
    });

    public void Call(int argumentsCount) => addInstruction(new()
    {
        Opcode = Opcode.Call,
        ArgumentValue = argumentsCount,
    });

    public void Ret() => addInstruction(new()
    {
        Opcode = Opcode.Ret,
    });

    public void BrcForw(IntermediateInstruction target) => addInstruction(new()
    {
        Opcode = Opcode.BrcForw,
        JumpTarget = target,
    });

    public void BrcBack(IntermediateInstruction target) => addInstruction(new()
    {
        Opcode = Opcode.BrcBack,
        JumpTarget = target,
    });

    public void BrPopTr(IntermediateInstruction target) => addInstruction(new()
    {
        Opcode = Opcode.BrPopTr,
        JumpTarget = target,
    });

    public void BrPopFl(IntermediateInstruction target) => addInstruction(new()
    {
        Opcode = Opcode.BrPopFl,
        JumpTarget = target,
    });

    public void BrPeekTr(IntermediateInstruction target) => addInstruction(new()
    {
        Opcode = Opcode.BrPeekTr,
        JumpTarget = target,
    });

    public void BrPeekFl(IntermediateInstruction target) => addInstruction(new()
    {
        Opcode = Opcode.BrPeekFl,
        JumpTarget = target,
    });

    public void BindFun() => addInstruction(new()
    {
        Opcode = Opcode.BindFun,
    });

    public void Pop(int count) => addInstruction(new()
    {
        Opcode = Opcode.Pop,
        ArgumentValue = count,
    });

    public void LdConst(int index) => addInstruction(new()
    {
        Opcode = Opcode.LdConst,
        ArgumentValue = index,
    });

    public void LdArg(int index) => addInstruction(new()
    {
        Opcode = Opcode.LdArg,
        ArgumentValue = index,
    });

    public void LdVar(int index) => addInstruction(new()
    {
        Opcode = Opcode.LdVar,
        ArgumentValue = index,
    });

    public void StVar(int index) => addInstruction(new()
    {
        Opcode = Opcode.StVar,
        ArgumentValue = index,
    });

    public void LdLocal(int index) => addInstruction(new()
    {
        Opcode = Opcode.LdLocal,
        ArgumentValue = index,
    });

    public void StLocal(int index) => addInstruction(new()
    {
        Opcode = Opcode.StLocal,
        ArgumentValue = index,
    });

    public void LdCell(int index) => addInstruction(new()
    {
        Opcode = Opcode.LdCell,
        ArgumentValue = index,
    });

    public void StCell(int index) => addInstruction(new()
    {
        Opcode = Opcode.StCell,
        ArgumentValue = index,
    });

    public void GAttr(int index) => addInstruction(new()
    {
        Opcode = Opcode.GAttr,
        ArgumentValue = index,
    });

    public void SAttr(int index) => addInstruction(new()
    {
        Opcode = Opcode.SAttr,
        ArgumentValue = index,
    });

    #endregion
}
