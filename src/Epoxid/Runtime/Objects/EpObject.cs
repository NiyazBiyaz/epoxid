namespace Epoxid.Runtime.Objects;

public class EpObject
{
    public EpType DunderClass { get; internal set; }

    public EpObject(EpType type)
    {
        DunderClass = type;
    }

    internal EpObject()
    {
        DunderClass = null!;
    }

    internal static EpBool DunderEqImplementation(EpObject self, EpObject other) => (EpBool)self.Equals(other);

    internal static EpBool DunderNeImplementation(EpObject self, EpObject other) => (EpBool)!self.Equals(other);
}
