using PySharp.Runtime;
using PySharp.Runtime.Objects;

namespace PySharp.VM;

internal class Engine
{
    private const int register_stack_count = 10_000;

    private readonly PsObject[] registerStack = new PsObject[register_stack_count];

    public void RunCode(CodeObject code)
    {
        var frame = registerStack.AsSpan(0, code.StackSize);

        bool stop = false;
        int programCounter = 0;
        while (!stop)
        {
            if (code.Instructions.Count <= programCounter)
            {
                throw new InvalidOperationException("Invalid code object: code never returns");
            }

            var instr = code.Instructions[programCounter];

            switch (instr.Opcode)
            {
                case Opcode.LdConst:
                    frame[instr.RegDest] = code.Constants[instr.Immediate16];
                    break;

                case Opcode.Ret:
                    frame.Clear();
                    stop = true;
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
    }
}
