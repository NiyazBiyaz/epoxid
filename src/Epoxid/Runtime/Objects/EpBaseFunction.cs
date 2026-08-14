namespace Epoxid.Runtime.Objects;

public abstract class EpBaseFunction(EpType type, string name) : EpObject(type)
{
    public string DunderName { get; } = name;
    public string QualName { get; init; } = name;

    public required FunctionParametersDescription ParamsDescription { get; init; }
}
