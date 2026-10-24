using System.Collections.Immutable;
using Epoxid.Runtime.Objects;

namespace Epoxid.VM;

public class CodeObject
{
    internal ImmutableArray<Instruction> Instructions { get; init; }
    public ImmutableArray<EpObject> Constants { get; init; }
    public ImmutableArray<string> VarNames { get; init; }
    public int LocalsCount { get; init; }

    internal CodeObject(
        ImmutableArray<Instruction> instructions,
        ImmutableArray<EpObject> constants,
        ImmutableArray<string> varNames,
        int localsCount)
    {
        Instructions = instructions;
        Constants = constants;
        VarNames = varNames;
        LocalsCount = localsCount;
    }
}
