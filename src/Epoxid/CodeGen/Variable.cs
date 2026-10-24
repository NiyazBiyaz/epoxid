namespace Epoxid.CodeGen;

internal class Variable
{
    public required VariableKind Kind { get; set; }
    public required VariableUsages Usage { get; set; }
    public required DeclarationScopeKind ScopeKind { get; init; }
    public required string Name { get; init; }

    public int LocalIndex { get; set; }

    public override string ToString() => $"Variable({Name}; {Kind}; {Usage}; {ScopeKind})";
}

internal enum VariableKind
{
    GlobalExplicit,
    GlobalImplicit,
    Local,
    Cell,
    Free,
}

[Flags]
internal enum VariableUsages
{
    Used = 1,
    DefinedLocal = 1 << 1,
    DefinedParam = 1 << 2,
    DefinedNonlocal = 1 << 3,
    DefinedGlobal = 1 << 4,
}

internal enum DeclarationScopeKind
{
    None,
    Global,
    LocalFree,
    LocalOnly,
    Closure,
}
