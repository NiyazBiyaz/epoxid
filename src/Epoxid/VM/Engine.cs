using Epoxid.Runtime;
using Epoxid.Runtime.Objects;

namespace Epoxid.VM;

public class Engine
{
    private const int register_stack_count = 10_000;

    private readonly EpObject[] registerStack = new EpObject[register_stack_count];

    private int stackCount = 0;

    // TODO: Probably change approach to something like thread-local Engine.
    // So this is why a private: we can add something like thread id to get/set methods.
    private static Engine currentEngine = null!;

    internal static void SetCurrentEngine(Engine engine) => currentEngine = engine;

    internal static Engine GetCurrentEngine() => currentEngine;

    public EpObject RunCode(CodeObject code, ReadOnlySpan<EpObject> argSpan, EpEnvironment environment)
    {
        if (currentEngine == null)
        {
            throw new InvalidOperationException("CurrentEngine is not set, execution of frames can cause an error.");
        }

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

            var current = code.Instructions[programCounter];

            switch (current.Opcode)
            {
                case Opcode.LdConst:
                    frame[current.RegDest] = code.Constants[current.Immediate16];
                    break;

                case Opcode.LdArg:
                    frame[current.RegDest] = argSpan[current.RegSrc1];
                    break;

                case Opcode.LdVar:
                {
                    var variable = environment.SearchVariable(code.VarNames[current.Immediate16])
                        ?? throw new Exception("NameError: TODO");

                    frame[current.RegDest] = variable;
                    break;
                }

                case Opcode.StVar:
                    environment.Module.Bind(code.VarNames[current.Immediate16], frame[current.RegDest]);
                    break;

                case Opcode.Ret:
                    result = frame[current.RegSrc1];
                    frame.Clear();
                    stop = true;
                    break;

                case Opcode.RetC:
                    result = code.Constants[current.Immediate16];
                    frame.Clear();
                    stop = true;
                    break;

                case Opcode.Call:
                {
                    var arguments = frame.Slice(current.RegDest + 1, current.RegSrc2);
                    var func = frame[current.RegSrc1];
                    frame[current.RegDest] = Core.CallFunction(func, arguments);
                    break;
                }

                case Opcode.CallK:
                {
                    throw new NotImplementedException("Functions with keyword arguments is not supported yet.");
                }

                case Opcode.Move:
                    frame[current.RegDest] = frame[current.RegSrc1];
                    break;

                case Opcode.Brc:
                    programCounter += current.Immediate16;
                    goto nextInstruction;

                case Opcode.BrTr:
                    if (Core.ConvertToBool(frame[current.RegDest]))
                    {
                        programCounter += current.Immediate16;
                        goto nextInstruction;
                    }
                    break;

                case Opcode.BrFl:
                    if (!Core.ConvertToBool(frame[current.RegDest]))
                    {
                        programCounter += current.Immediate16;
                        goto nextInstruction;
                    }
                    break;

                // Register-to-register section
                case Opcode.Add:
                    frame[current.RegDest] = Core.AddObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.Sub:
                    frame[current.RegDest] = Core.SubtractObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.Mul:
                    frame[current.RegDest] = Core.MultiplyObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.TDiv:
                    frame[current.RegDest] = Core.TrueDivideObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.Mod:
                    frame[current.RegDest] = Core.ModuleObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.Eq:
                    frame[current.RegDest] = Core.EqualObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.NEq:
                    frame[current.RegDest] = Core.NotEqualObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.LsTh:
                    frame[current.RegDest] = Core.LessThanObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.GrTh:
                    frame[current.RegDest] = Core.GreaterThanObjects(frame[current.RegSrc1], frame[current.RegSrc2]);
                    break;

                case Opcode.InAdd:
                {
                    if (!Core.TryInPlaceAdd(frame[current.RegDest], frame[current.RegSrc1], out var value))
                    {
                        value = Core.AddObjects(frame[current.RegDest], frame[current.RegSrc1]);
                    }
                    frame[current.RegDest] = value;
                    break;
                }

                case Opcode.InSub:
                {
                    if (!Core.TryInPlaceSubtract(frame[current.RegDest], frame[current.RegSrc1], out var value))
                    {
                        value = Core.SubtractObjects(frame[current.RegDest], frame[current.RegSrc1]);
                    }
                    frame[current.RegDest] = value;
                    break;
                }

                case Opcode.InMul:
                {
                    if (!Core.TryInPlaceMultiply(frame[current.RegDest], frame[current.RegSrc1], out var value))
                    {
                        value = Core.MultiplyObjects(frame[current.RegDest], frame[current.RegSrc1]);
                    }
                    frame[current.RegDest] = value;
                    break;
                }

                case Opcode.InTDiv:
                {
                    if (!Core.TryInPlaceTrueDivide(frame[current.RegDest], frame[current.RegSrc1], out var value))
                    {
                        value = Core.TrueDivideObjects(frame[current.RegDest], frame[current.RegSrc1]);
                    }
                    frame[current.RegDest] = value;
                    break;
                }

                case Opcode.InMod:
                {
                    if (!Core.TryInPlaceModule(frame[current.RegDest], frame[current.RegSrc1], out var value))
                    {
                        value = Core.ModuleObjects(frame[current.RegDest], frame[current.RegSrc1]);
                    }
                    frame[current.RegDest] = value;
                    break;
                }

                default:
                    throw new InvalidOperationException($"Invalid opcode value: {current.Opcode}");
            }

            programCounter += 1;
        }

        stackCount -= code.StackSize;
        return result;
    }
}
