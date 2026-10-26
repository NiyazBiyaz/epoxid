namespace Epoxid.Runtime.Objects;

public class WrapperObject<T> : EpObject
{
    public T Value { get; set; }

    public WrapperObject(T value)
        : base(Wrapper)
    {
        Value = value;
    }

    public static readonly EpType Wrapper = new("wrapper object", [Object], EpType.Type);

    public override string ToString() => $"<wrapper object for '{Value}' CLR value>";
}
