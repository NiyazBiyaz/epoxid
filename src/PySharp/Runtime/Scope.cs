using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

public class Scope
{
    private readonly Dictionary<string, PsObject> variables = [];

    public void Bind(string name, PsObject value)
    {
        Debug.Assert(name != null);

        variables[name] = value;
    }

    public void Unbind(string name)
    {
        Debug.Assert(name != null);

        variables.Remove(name);
    }

    public bool TryGetValue(ReadOnlySpan<char> name, [NotNullWhen(true)] out PsObject? value)
    {
        var lookup = variables.GetAlternateLookup<ReadOnlySpan<char>>();
        return lookup.TryGetValue(name, out value);
    }
}
