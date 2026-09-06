namespace Epoxid.CodeGenV2;

internal class Register : IEquatable<Register>
{
    public int? Address = null;


    public IntermediateInstruction? LastUsage { get; set; } = null;

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

#if DEBUG
    public required int Mnemonics { get; init; }
    public override string ToString() => $"r{Mnemonics}";
#endif
}
