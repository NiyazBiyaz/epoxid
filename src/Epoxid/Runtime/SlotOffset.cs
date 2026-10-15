namespace Epoxid.Runtime;

public readonly struct SlotOffset(int value) : IEquatable<SlotOffset>
{
    public int Value => value;

    public override string ToString() => $"SlotOffset{Value}";

    public bool Equals(SlotOffset other) => other.Value == Value;

    public static bool operator ==(SlotOffset left, SlotOffset right) => left.Equals(right);
    public static bool operator !=(SlotOffset left, SlotOffset right) => !left.Equals(right);

    public override bool Equals(object? obj) => obj is SlotOffset offset && Equals(offset);

    public override int GetHashCode() => Value.GetHashCode();
}
