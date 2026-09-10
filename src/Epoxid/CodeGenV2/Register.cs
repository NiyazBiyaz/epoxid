namespace Epoxid.CodeGenV2;

internal class Register : IEquatable<Register>
{
    public int? Address = null;

    public int? CallId { get; set; }

    public int? CallRelativeAddress { get; set; }

    public int? CallCount { get; set; }

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

    public int ColoringSortScore => LifetimeCollisions.Count + (CallId == null ? 0 : 1) * 8192;

    public required int Id { get; init; }
    public override string ToString() => $"r{Id}";
}
