using Epoxid.VM;

namespace Epoxid.Runtime.Objects;

public class EpFunction(string name, CodeObject code) : EpBaseFunction(Function, name)
{
    internal readonly static EpType Function = new("function", [Object], EpType.Type);

    public CodeObject Code { get; } = code;

    public EpEnvironment? Environment { get; set; } = null;
}
