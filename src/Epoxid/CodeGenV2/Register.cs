namespace Epoxid.CodeGenV2;

internal class Register : IEquatable<Register>
{
    public int? Address = null;

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
}
