using System.Diagnostics;

namespace Epoxid.VM;

[DebuggerDisplay("{getDebuggerDisplay(),nq}")]
internal readonly struct Instruction
{
    private readonly ushort rawData;

    public Opcode Opcode => (Opcode)(rawData & byte.MaxValue);

    public byte Argument => (byte)(rawData >> 8 & byte.MaxValue);

    public Instruction(Opcode opcode)
    {
        rawData = (ushort)opcode;
    }

    public Instruction(Opcode opcode, byte argument)
    {
        rawData = (ushort)opcode;
        rawData |= (ushort)(argument << 8);
    }

    public override string ToString() => $"{Opcode}({Argument})";

    private string getDebuggerDisplay() => ToString();
}
