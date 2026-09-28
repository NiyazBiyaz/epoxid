using Epoxid.Runtime.Objects;

namespace Epoxid.Runtime;

public readonly record struct EpEnvironment
{
    public required Scope Builtins { get; init; }
    public required Scope Module { get; init; }

    public readonly EpObject? SearchVariable(string name)
    {
        if (Module.TryGetValue(name, out var obj))
        {
            return obj;
        }
        if (Builtins.TryGetValue(name, out obj))
        {
            return obj;
        }

        return null;
    }
}
