namespace PySharp.VM;

internal readonly struct Instruction
{
    private readonly uint rawData;

    private const int opcode_offset = 24;
    private const int reg_dest_offset = 16;
    private const int reg_src1_offset = 8;
    private const int imm24_sign_bit = 1 << 23;
    private const int imm24_mask = (byte.MaxValue << 15) | ushort.MaxValue;
    private const int imm24_min_value = -8_388_608; // -2**23
    private const int imm24_max_value = 8_388_607; // 2**23 - 1

    public Opcode Opcode => (Opcode)(rawData >> opcode_offset);

    public int RegDest => (int)(rawData >> reg_dest_offset & byte.MaxValue);

    public int RegSrc1 => (int)(rawData >> reg_src1_offset & byte.MaxValue);

    public int RegSrc2 => (int)(rawData & byte.MaxValue);

    public int Immediate16 => (short)(rawData & ushort.MaxValue);

    public int Immediate24
    {
        get
        {
            int result = (int)(rawData & imm24_mask);
            if ((rawData & imm24_sign_bit) != 0)
            {
                result = ~result;
            }

            return result;
        }
    }

    public Instruction(Opcode opcode, byte regDest, byte regSrc1, byte regSrc2)
    {
        rawData |= ((uint)opcode) << opcode_offset;
        rawData |= ((uint)regDest) << reg_dest_offset;
        rawData |= ((uint)regSrc1) << reg_src1_offset;
        rawData |= regSrc2;
    }

    public Instruction(Opcode opcode, byte regDest, short immediate)
    {
        rawData |= ((uint)opcode) << opcode_offset;
        rawData |= ((uint)regDest) << reg_dest_offset;
        rawData |= (ushort)immediate;
    }

    public Instruction(Opcode opcode, int immediate)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(immediate, imm24_max_value);
        ArgumentOutOfRangeException.ThrowIfLessThan(immediate, imm24_min_value);

        int immediateValue = 0;
        if (int.IsNegative(immediate))
        {
            immediateValue |= imm24_sign_bit;
            immediateValue |= ~immediate;
        }
        else
        {
            immediateValue |= immediate & imm24_mask;
        }

        rawData |= ((uint)opcode) << opcode_offset;
        rawData |= (uint)immediateValue;
    }
}
