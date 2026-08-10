using PySharp.VM;

namespace PySharp.Tests.VM;

public class TestInstruction
{
    [Fact]
    public void Test_R2RMode()
    {
        var instr = new Instruction(Opcode.Mul, 42, 52, 254);

        Assert.Equal(Opcode.Mul, instr.Opcode);
        Assert.Equal(42, instr.RegDest);
        Assert.Equal(52, instr.RegSrc1);
        Assert.Equal(254, instr.RegSrc2);
    }

    [Fact]
    public void Test_ImmMode()
    {
        var instr = new Instruction(Opcode.LdConst, 69, 123);

        Assert.Equal(Opcode.LdConst, instr.Opcode);
        Assert.Equal(69, instr.RegDest);
        Assert.Equal(123, instr.Immediate16);
    }

    [Fact]
    public void Test_ImmMode_NegativeValue()
    {
        var instr = new Instruction(Opcode.LdConst, 69, -123);

        Assert.Equal(Opcode.LdConst, instr.Opcode);
        Assert.Equal(69, instr.RegDest);
        Assert.Equal(-123, instr.Immediate16);
    }

    [Fact]
    public void Test_Imm24Mode()
    {
        var instr = new Instruction(Opcode.LJmp, 1234567);

        Assert.Equal(Opcode.LJmp, instr.Opcode);
        Assert.Equal(1234567, instr.Immediate24);
    }

    [Fact]
    public void Test_Imm24Mode_NegativeValue()
    {
        var instr = new Instruction(Opcode.LJmp, -1234567);

        Assert.Equal(Opcode.LJmp, instr.Opcode);
        Assert.Equal(-1234567, instr.Immediate24);
    }

    [Fact]
    public void Test_Imm24Mode_OverflowedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Instruction(Opcode.LJmp, 10_000_000);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Instruction(Opcode.LJmp, -10_000_000);
        });
    }
}
