using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Epoxid.Runtime;
using Epoxid.Runtime.Objects;

namespace Epoxid.VM;

internal class Engine
{
    private const int initial_stack_size = 64_000;

    // TODO: thread-local engines.
    public static Engine CurrentEngine { get; set; } = new();

    private readonly EpObject?[] stack = new EpObject[initial_stack_size];

    /// <summary>
    /// Count of the value on stack.
    /// </summary>
    private int stackPointer = 0;

    public EpObject RunCode(CodeObject code, ReadOnlySpan<EpObject> arguments, EpEnvironment environment)
    {
        if (CurrentEngine == null)
        {
            throw new WarningException("CurrentEngine is not set, execution of new frames can cause an error");
        }

        EpObject ret = null!;

        var locals = stack.AsSpan(stackPointer, code.LocalsCount);
        arguments.CopyTo(locals!);
        stackPointer += code.LocalsCount;

        bool stop = false;
        int programCounter = 0;
        int extendedArgsRegister = 0;
        while (!stop)
        {
            if (programCounter >= code.Instructions.Length)
            {
                throw new ArgumentException("Invalid code object: code never returns");
            }
            if (programCounter < 0)
            {
                throw new ArgumentException("Invalid code object: program counter became negative while executing");
            }

            var current = code.Instructions[programCounter];

            switch (current.Opcode)
            {
                #region Binary operations

                case Opcode.Add:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.AddObjects(left, right));
                    break;
                }
                case Opcode.Sub:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.SubtractObjects(left, right));
                    break;
                }
                case Opcode.Mul:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.MultiplyObjects(left, right));
                    break;
                }
                case Opcode.TDiv:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.TrueDivideObjects(left, right));
                    break;
                }
                case Opcode.Mod:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.ModuleObjects(left, right));
                    break;
                }
                case Opcode.Eq:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.EqualObjects(left, right));
                    break;
                }
                case Opcode.NEq:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.NotEqualObjects(left, right));
                    break;
                }
                case Opcode.GrTh:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.GreaterThanObjects(left, right));
                    break;
                }
                case Opcode.LsTh:
                {
                    EpObject right = pop(), left = pop();
                    push(Core.LessThanObjects(left, right));
                    break;
                }
                case Opcode.InAdd:
                {
                    EpObject target = pop(), rhs = pop();
                    if (Core.TryInPlaceAdd(target, rhs, out var res))
                        push(res);
                    else
                        push(Core.AddObjects(target, rhs));
                    break;
                }
                case Opcode.InSub:
                {
                    EpObject target = pop(), rhs = pop();
                    if (Core.TryInPlaceSubtract(target, rhs, out var res))
                        push(res);
                    else
                        push(Core.SubtractObjects(target, rhs));
                    break;
                }
                case Opcode.InMul:
                {
                    EpObject target = pop(), rhs = pop();
                    if (Core.TryInPlaceMultiply(target, rhs, out var res))
                        push(res);
                    else
                        push(Core.MultiplyObjects(target, rhs));
                    break;
                }
                case Opcode.InTDiv:
                {
                    EpObject target = pop(), rhs = pop();
                    if (Core.TryInPlaceTrueDivide(target, rhs, out var res))
                        push(res);
                    else
                        push(Core.TrueDivideObjects(target, rhs));
                    break;
                }
                case Opcode.InMod:
                {
                    EpObject target = pop(), rhs = pop();
                    if (Core.TryInPlaceModule(target, rhs, out var res))
                        push(res);
                    else
                        push(Core.ModuleObjects(target, rhs));
                    break;
                }

                #endregion
                #region Special operations

                case Opcode.Call:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int argCount = getAndErase(ref extendedArgsRegister);

                    var callValues = stack.AsSpan(stackPointer - argCount - 1, argCount + 1);

                    var args = callValues[1..];
                    var function = callValues[0];

                    Debug.Assert(function != null);
                    Debug.Assert(!args.Contains(null));

                    var res = Core.CallObject(function, args!);

                    stackPointer -= argCount + 1;
                    callValues.Clear();

                    push(res);
                    break;
                }
                case Opcode.Ret:
                    ret = pop();
                    stop = true;
                    break;

                case Opcode.BrcForw:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int jumpDelta = getAndErase(ref extendedArgsRegister);

                    programCounter += jumpDelta;

                    break;
                }
                case Opcode.BrcBack:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int jumpDelta = getAndErase(ref extendedArgsRegister);

                    programCounter -= jumpDelta;

                    break;
                }
                case Opcode.BrPopTr:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int jumpDelta = getAndErase(ref extendedArgsRegister);

                    var value = pop();
                    if (Core.ConvertToBool(value))
                    {
                        programCounter += jumpDelta;
                    }
                    break;
                }
                case Opcode.BrPopFl:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int jumpDelta = getAndErase(ref extendedArgsRegister);

                    var value = pop();
                    if (!Core.ConvertToBool(value))
                    {
                        programCounter += jumpDelta;
                    }
                    break;
                }
                case Opcode.BrPeekTr:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int jumpDelta = getAndErase(ref extendedArgsRegister);

                    var value = peek();
                    if (Core.ConvertToBool(value))
                    {
                        programCounter += jumpDelta;
                    }
                    break;
                }
                case Opcode.BrPeekFl:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int jumpDelta = getAndErase(ref extendedArgsRegister);

                    var value = peek();
                    if (!Core.ConvertToBool(value))
                    {
                        programCounter += jumpDelta;
                    }
                    break;
                }
                case Opcode.BindFun:
                {
                    var value = peek();

                    if (value is not EpFunction function)
                        throw new InvalidCodeException("value is not a function");

                    // TODO: add some stuff with cells etc
                    function.Environment = environment;
                    break;
                }

                #endregion
                #region Load/Store operations

                case Opcode.LdConst:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int constantIndex = getAndErase(ref extendedArgsRegister);

                    push(code.Constants[constantIndex]);
                    break;
                }
                case Opcode.LdArg:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int argumentIndex = getAndErase(ref extendedArgsRegister);

                    push(arguments[argumentIndex]);
                    break;
                }
                case Opcode.LdVar:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int nameIndex = getAndErase(ref extendedArgsRegister);
                    string name = code.VarNames[nameIndex];

                    var var = environment.SearchVariable(name);
                    if (var == null)
                    {
                        throw new NotImplementedException("Errors in engine.");
                    }
                    else
                        push(var);
                    break;
                }
                case Opcode.StVar:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int nameIndex = getAndErase(ref extendedArgsRegister);
                    string name = code.VarNames[nameIndex];

                    environment.Module.Bind(name, pop());
                    break;
                }
                case Opcode.LdLocal:
                {
                    var value = locals[current.Argument] ?? throw new Exception("TODO: NameError");
                    push(value);
                    break;
                }
                case Opcode.StLocal:
                {
                    var value = pop();
                    locals[current.Argument] = value;
                    break;
                }
                case Opcode.LdCell:
                {
                    throw new NotImplementedException("LdCell");
                }
                case Opcode.StCell:
                {
                    throw new NotImplementedException("StCell");
                }
                case Opcode.GAttr:
                {
                    addExtendedArg(ref extendedArgsRegister, 0, current.Argument);

                    int nameIndex = getAndErase(ref extendedArgsRegister);
                    string name = code.VarNames[nameIndex];

                    var stem = peek();
                    var value = Core.GetAttribute(stem, (EpString)name); // TODO: string interning?
                    push(value);

                    break;
                }
                case Opcode.SAttr:
                {
                    throw new NotImplementedException();
                }

                #endregion
                #region Extended arguments

                case Opcode.Ext1:
                    addExtendedArg(ref extendedArgsRegister, 1, current.Argument);
                    break;

                case Opcode.Ext2:
                    addExtendedArg(ref extendedArgsRegister, 2, current.Argument);
                    break;

                case Opcode.Ext3:
                    addExtendedArg(ref extendedArgsRegister, 3, current.Argument);
                    break;

                #endregion
            }

            programCounter += 1;
        }

        locals.Clear();
        stackPointer -= locals.Length;

        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EpObject pop()
    {
        if (stackPointer < 1)
            throw new InvalidOperationException("Cannot pop from stack: no values");

        stackPointer -= 1;
        var value = stack[stackPointer] ?? throw new NullReferenceException("Value on stack is null");
        stack[stackPointer] = null;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EpObject peek()
    {
        if (stackPointer < 1)
            throw new InvalidOperationException("Cannot peek on stack: no values");

        var value = stack[stackPointer - 1] ?? throw new NullReferenceException("Value on stack is null");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void push(EpObject value)
    {
        Debug.Assert(value != null);

        if (stackPointer < stack.Length)
        {
            stack[stackPointer] = value;
            stackPointer += 1;
        }
        else
        {
            throw new EngineOverflowException(stack.Length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void addExtendedArg(ref int register, int octet, byte value)
    {
        Debug.Assert(octet is >= 0 and <= 3);

        register |= value << (octet * 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int getAndErase(ref int register)
    {
        int value = register;
        register = 0;
        return value;
    }
}
