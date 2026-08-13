using System.Collections.Immutable;
using PySharp.Runtime.Objects;

namespace PySharp.VM;

internal class CodeObject
{
    public required ImmutableArray<Instruction> Instructions { get; init; }
    public required ImmutableArray<PsObject> Constants { get; init; }
    public required ImmutableArray<string> VarNames { get; init; }
    public required int StackSize { get; init; }
}
