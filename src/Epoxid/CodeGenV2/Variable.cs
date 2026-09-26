namespace Epoxid.CodeGenV2;

public class Variable
{
    public required string Name { get; init; }

    public VariableKind Kind { get; set; }

    internal Register? Register { get; set; }
}

public enum VariableKind
{
    Local,
    Cell,
    Global,
}
