namespace Epoxid.CodeGen;

public class Variable
{
    public required string Name { get; init; }

    public VariableKind Kind { get; set; }

    internal Register? Register { get; set; }

    internal int? ParameterPosition { get; set; }
}

public enum VariableKind
{
    Local,
    Cell,
    Global,
}
