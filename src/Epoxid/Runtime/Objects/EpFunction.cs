namespace Epoxid.Runtime.Objects;

public class EpFunction(string name) : EpBaseFunction(EpConstants.Function, name)
{
    /* Here should be function implementation, but not now. */
    internal readonly static EpType Type = new("function", [EpConstants.Object], EpConstants.Type);
}
