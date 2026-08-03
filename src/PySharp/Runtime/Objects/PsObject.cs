namespace PySharp.Runtime.Objects;

public class PsObject
{
    public PsType DunderClass { get; internal set; }

    public PsObject(PsType type)
    {
        DunderClass = type;
    }

    internal PsObject()
    {
        DunderClass = null!;
    }

    internal static PsBool DunderEqImplementation(PsObject self, PsObject other) => (PsBool)self.Equals(other);

    internal static PsBool DunderNeImplementation(PsObject self, PsObject other) => (PsBool)!self.Equals(other);
}
