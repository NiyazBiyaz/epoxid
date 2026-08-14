using Epoxid.Runtime.Objects;

namespace Epoxid.Runtime;

public readonly struct Environment
{
    public Environment()
    {
    }

    public Stack<Scope> Scopes { get; } = [];

    public readonly EpObject? SearchVariable(string name)
    {
        foreach (var scope in Scopes)
        {
            if (scope.TryGetValue(name, out var obj))
            {
                return obj;
            }
        }

        return null;
    }
}
