using Epoxid.VM;

namespace Epoxid.Runtime.Objects;

public class EpFunction(string name, CodeObject code) : EpBaseFunction(EpConstants.Function, name)
{
    internal readonly static EpType Type = new("function", [EpConstants.Object], EpConstants.Type);

    public CodeObject Code { get; } = code;

    public EpEnvironment? Environment { get; set; } = null;
}
