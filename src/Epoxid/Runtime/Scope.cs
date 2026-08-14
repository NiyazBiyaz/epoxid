using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Epoxid.Runtime.Objects;

namespace Epoxid.Runtime;

public class Scope
{
    private readonly Dictionary<string, EpObject> variables = [];

    public void Bind(string name, EpObject value)
    {
        Debug.Assert(name != null);

        variables[name] = value;
    }

    public void Unbind(string name)
    {
        Debug.Assert(name != null);

        variables.Remove(name);
    }

    public bool TryGetValue(ReadOnlySpan<char> name, [NotNullWhen(true)] out EpObject? value)
    {
        var lookup = variables.GetAlternateLookup<ReadOnlySpan<char>>();
        return lookup.TryGetValue(name, out value);
    }
}
