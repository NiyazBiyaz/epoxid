using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PySharp.SyntaxAnalysis.Common;

public static class NumberParser
{
    /// <summary>
    /// Helper method to be able parse escape sequences in <see cref="StringParser"/>.
    /// </summary>
    /// <param name="span">Chars of the escape sequence.</param>
    /// <param name="digitBase">Digit base. May be 16 or 8.</param>
    /// <returns><paramref name="span"/> as <see langword="uint"/> on success; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Only supported bases are 16 and 8.</exception>
    internal static uint? ParseChars(ReadOnlySpan<char> span, int digitBase)
    {
        Debug.Assert(span.Length <= 8);

        int shift = digitBase switch
        {
            16 => 4,
            8 => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(digitBase), digitBase, "only supported bases are 16 and 8")
        };

        uint result = 0;
        foreach (char c in span)
        {
            var val = convert(c);
            if (val is null || (uint)val.Value >= digitBase)
                return null;

            result = (result << shift) | (uint)val.Value;
        }

        return result;
    }

    public static long ParseInteger(ReadOnlySpan<char> number) => number switch
    {
        ['0', 'x'] => long.Parse(number, NumberStyles.HexNumber, CultureInfo.InvariantCulture),

        ['0', 'b'] => long.Parse(number, NumberStyles.BinaryNumber, CultureInfo.InvariantCulture),

        ['0', 'o'] => throw new NotImplementedException("Octal numbers cannot be parsed yet."),

        _ => long.Parse(number, CultureInfo.InvariantCulture),
    };

    public static double ParseFloat(ReadOnlySpan<char> number) => double.Parse(number, CultureInfo.InvariantCulture);

    public static Complex ParseComplex(ReadOnlySpan<char> number) => Complex.Parse(number, CultureInfo.InvariantCulture);

    public static NumberType GetNumberType(ReadOnlySpan<char> number) => number switch
    {
        ['0', 'x'] or ['0', 'b'] or ['0', 'o'] => NumberType.Integer,
        _ when number.ContainsAny("jJ") => NumberType.Complex,
        _ when number.ContainsAny("eE.") => NumberType.Float,
        _ => NumberType.Integer,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int? convert(char ch) => ch switch
    {
        >= '0' and <= '9' => ch - '0',
        >= 'a' and <= 'f' => ch - 'a' + 10,
        >= 'A' and <= 'F' => ch - 'A' + 10,
        _ => null,
    };
}

public enum NumberType
{
    Integer,
    Float,
    Complex,
}
