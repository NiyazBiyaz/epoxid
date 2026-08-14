using System.Collections.Immutable;
using Epoxid.Runtime.Objects;

namespace Epoxid.VM;

internal class CodeObject
{
    public required ImmutableArray<Instruction> Instructions { get; init; }
    public required ImmutableArray<EpObject> Constants { get; init; }
    public required ImmutableArray<string> VarNames { get; init; }
    public required int StackSize { get; init; }
}
