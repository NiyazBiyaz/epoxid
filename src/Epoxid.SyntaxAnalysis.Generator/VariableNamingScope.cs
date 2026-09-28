using Humanizer;

namespace Epoxid.SyntaxAnalysis.Generator;

internal class VariableNamingScope
{
    private readonly HashSet<string> existing = [];

    public string NextName(string original)
    {
        string name = original = original.Underscore();
        int suffixNumber = 1;
        while (existing.Contains(name))
            name = original + suffixNumber++;

        existing.Add(name);
        return name;
    }

    public string NextNamePreserveCase(string original)
    {
        string name = original;
        int suffixNumber = 1;
        while (existing.Contains(name))
            name = original + suffixNumber++;

        existing.Add(name);
        return name;
    }

    public string NextTypeName() => NextNamePreserveCase("_PegenNetAnonymousType");
}
