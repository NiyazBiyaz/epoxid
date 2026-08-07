namespace PySharp.Runtime.Objects;

public abstract class PsBaseFunction(PsType type, string name) : PsObject(type)
{
    public string DunderName { get; } = name;
    public string QualName { get; init; } = name;

    public required FunctionParametersDescription ParamsDescription { get; init; }
}
