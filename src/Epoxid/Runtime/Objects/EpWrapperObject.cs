namespace Epoxid.Runtime.Objects;

public class EpWrapperObject(object? obj) : EpObject
{
    public object? Value = obj;

    public override string ToString() => Value?.ToString() ?? "null";
}
