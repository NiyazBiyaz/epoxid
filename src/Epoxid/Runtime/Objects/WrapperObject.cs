namespace Epoxid.Runtime.Objects;

public class WrapperObject<T> : EpObject
{
    public T Value { get; set; }

    public WrapperObject(T value)
        : base(Wrapper)
    {
        Value = value;
    }

    public static readonly EpType Wrapper = new("native objects wrapper", [Object], EpType.Type);

    public override string ToString() => $"<wrapper for '{Value}' object>";
}
