using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

public class Scope
{
    private readonly Dictionary<string, PsObject> variables = [];
    private readonly Stack<bool> loopsStack = [];

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

    /// <summary>
    /// Create new loop entry in the current execution scope
    /// </summary>
    public void SetupLoop() => loopsStack.Push(false);

    /// <summary>
    /// Set <i>break</i> flag for the currently active loop
    /// </summary>
    public void SetBreakFlag()
    {
        if (loopsStack.Count == 0)
        {
            throw new InvalidOperationException("Can't set 'break' flag: call SetupLoop() first.");
        }

        loopsStack.Pop();
        loopsStack.Push(true);
    }

    /// <summary>
    /// Terminate currently active loop and return it's <i>break</i> flag
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if <i>break</i> flag was set before, otherwise <see langword="false"/>
    /// </returns>
    public bool TerminateLoop()
    {
        if (loopsStack.Count == 0)
        {
            throw new InvalidOperationException("Can't terminate loop: call SetupLoop() first.");
        }

        return loopsStack.Pop();
    }

    public bool PeekLoop()
    {
        if (loopsStack.Count == 0)
        {
            throw new InvalidOperationException("Can't peek loop state: call SetupLoop() first.");
        }

        return loopsStack.Peek();
    }
}
