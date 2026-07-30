using PySharp.SyntaxAnalysis.Common;

namespace PySharp.SyntaxAnalysis.Tests.Common;

public class TestNumberParser
{
    [Theory]
    [InlineData("123", NumberType.Integer)]
    [InlineData("0", NumberType.Integer)]
    [InlineData("0x123", NumberType.Integer)]
    [InlineData("0o255", NumberType.Integer)]
    [InlineData("0b1010", NumberType.Integer)]
    [InlineData("10.0", NumberType.Float)]
    [InlineData(".10", NumberType.Float)]
    [InlineData("10.", NumberType.Float)]
    [InlineData("10e1", NumberType.Float)]
    [InlineData("10E2", NumberType.Float)]
    [InlineData("10.2e1", NumberType.Float)]
    [InlineData("10.0j", NumberType.Complex)]
    [InlineData(".10j", NumberType.Complex)]
    [InlineData("10.j", NumberType.Complex)]
    [InlineData("10e1J", NumberType.Complex)]
    [InlineData("10E2j", NumberType.Complex)]
    [InlineData("10.2e1J", NumberType.Complex)]
    public void TestNumbersIdentifying(string number, NumberType expected)
    {
        var actual = NumberParser.GetNumberType(number);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("0", 0)]
    [InlineData("0x123", 0x123)]
    [InlineData("0xfafa", 0xfafa)]
    [InlineData("0o255", 175, Skip = "Not implemented yet.")]
    [InlineData("0b1010", 0b1010)]
    public void TestParseInteger(string number, long expected)
    {
        long actual = NumberParser.ParseInteger(number);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("123.123", 123.123)]
    [InlineData("123e1", 123e1)]
    [InlineData("123e-1", 123e-1)]
    [InlineData("123e+2", 123e+2)]
    [InlineData(".12", .12)]
    [InlineData("123.", 123)]
    [InlineData(".12e3", .12e3)]
    public void TestParseFloat(string number, double expected)
    {
        double actual = NumberParser.ParseFloat(number);

        Assert.Equal(expected, actual);
    }
}
