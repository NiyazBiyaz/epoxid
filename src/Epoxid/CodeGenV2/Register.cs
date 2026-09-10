namespace Epoxid.CodeGenV2;

internal class Register : IEquatable<Register>
{
    public int? Address = null;

    public UsageSpan Usage { get; } = new();

    public List<Register> LifetimeCollisions { get; } = [];

    public bool Equals(Register? other)
    {
        if (other == null)
        {
            return false;
        }

        if (Address != null && other.Address != null)
        {
            return Address == other.Address;
        }

        return ReferenceEquals(this, other);
    }

    public required int Id { get; init; }
    public override string ToString() => $"r{Id}";
}
