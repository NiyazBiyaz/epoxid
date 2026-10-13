namespace Epoxid.Runtime;

public class SlotOffset(int value) : IEquatable<SlotOffset>
{
    public int Value => value;

    public override string ToString() => $"SlotOffset{Value}";

    public bool Equals(SlotOffset? other) => other switch
    {
        { Value: int otherValue } => otherValue == Value,
        _ => false,
    };

    public override bool Equals(object? obj) => obj is SlotOffset offset && Equals(offset);

    public override int GetHashCode() => Value.GetHashCode();
}
