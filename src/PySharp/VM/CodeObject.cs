using PySharp.Runtime.Objects;

namespace PySharp.VM;

internal class CodeObject
{
    public List<Instruction> Instructions { get; init; } = [];
    public List<PsObject> Constants { get; init; } = [];
    public List<string> VarNames { get; init; } = [];
    public int StackSize { get; set; }
    public int ArgCount { get; set; }

    public int FrameSize => StackSize + ArgCount;
}
