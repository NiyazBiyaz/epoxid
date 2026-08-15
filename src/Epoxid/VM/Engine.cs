using Epoxid.Runtime;
using Epoxid.Runtime.Objects;

namespace Epoxid.VM;

internal class Engine
{
    private const int register_stack_count = 10_000;

    private readonly EpObject[] registerStack = new EpObject[register_stack_count];

    private int stackCount = 0;

    public EpObject RunCode(CodeObject code, ReadOnlySpan<EpObject> argSpan, Runtime.Environment environment)
    {
        EpObject result = default!;
        var frame = registerStack.AsSpan(stackCount, code.StackSize);
        stackCount += code.StackSize;

        bool stop = false;
        int programCounter = 0;
        while (!stop)
        {
        nextInstruction: // To avoid programCounter auto-incrementing after branches
            if (code.Instructions.Length <= programCounter)
            {
                throw new ArgumentException("Invalid code object: code never returns");
            }

            var instr = code.Instructions[programCounter];

            switch (instr.Opcode)
            {
                case Opcode.LdConst:
                    frame[instr.RegDest] = code.Constants[instr.Immediate16];
                    break;

                case Opcode.LdArg:
                    frame[instr.RegDest] = argSpan[instr.RegSrc1];
                    break;

                case Opcode.LdVar:
                    var variable = environment.SearchVariable(code.VarNames[instr.Immediate16])
                        ?? throw new Exception("NameError: TODO");

                    frame[instr.RegDest] = variable;
                    break;

                case Opcode.Ret:
                    result = frame[instr.RegSrc1];
                    frame.Clear();
                    stop = true;
                    break;

                case Opcode.RetC:
                    result = code.Constants[instr.Immediate16];
                    frame.Clear();
                    stop = true;
                    break;

                case Opcode.Call:
                {
                    var arguments = frame.Slice(instr.RegDest, instr.RegSrc2);
                    var func = frame[instr.RegSrc1];
                    frame[instr.RegDest] = Core.CallFunction(func, arguments);
                    break;
                }

                case Opcode.CallK:
                {
                    var arguments = frame.Slice(instr.RegDest, instr.RegSrc2);
                    var keywordArgs = frame[instr.RegDest + instr.RegSrc2];
                    var func = frame[instr.RegSrc1];
                    frame[instr.RegDest] = Core.CallKeywordFunction(func, arguments, keywordArgs);
                    break;
                }

                case Opcode.Move:
                    frame[instr.RegDest] = frame[instr.RegSrc1];
                    break;

                case Opcode.Brc:
                    programCounter += instr.Immediate24;
                    goto nextInstruction;

                case Opcode.BrTr:
                    if (Core.ConvertBool(frame[instr.RegDest]))
                    {
                        programCounter += instr.Immediate16;
                        goto nextInstruction;
                    }
                    break;

                case Opcode.BrFl:
                    if (!Core.ConvertBool(frame[instr.RegDest]))
                    {
                        programCounter += instr.Immediate16;
                        goto nextInstruction;
                    }
                    break;

                // Register-to-register section
                case Opcode.Add:
                    frame[instr.RegDest] = Core.AddObjects(frame[instr.RegSrc1], frame[instr.RegSrc2]);
                    break;

                case Opcode.Sub:
                    frame[instr.RegDest] = Core.SubtractObjects(frame[instr.RegSrc1], frame[instr.RegSrc2]);
                    break;

                case Opcode.Mul:
                    frame[instr.RegDest] = Core.MultiplyObjects(frame[instr.RegSrc1], frame[instr.RegSrc2]);
                    break;

                case Opcode.TDiv:
                    frame[instr.RegDest] = Core.TrueDivideObjects(frame[instr.RegSrc1], frame[instr.RegSrc2]);
                    break;

                case Opcode.Eq:
                    frame[instr.RegDest] = Core.EqualObjects(frame[instr.RegSrc1], frame[instr.RegSrc2]);
                    break;

                case Opcode.NEq:
                    frame[instr.RegDest] = Core.NotEqualObjects(frame[instr.RegSrc1], frame[instr.RegSrc2]);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid opcode value: {instr.Opcode}");
            }

            programCounter += 1;
        }

        stackCount -= code.StackSize;
        return result;
    }
}
