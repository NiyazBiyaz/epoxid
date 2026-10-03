namespace Epoxid.Runtime.Objects;

public partial class EpObject
{
    public EpType DunderClass { get; internal set; }

    private protected Memory<object?> DunderSlots { get; set; }

#pragma warning disable IDE1006 // Naming Styles

    protected const int _EpObjectSlotCount = 1;

#pragma warning restore IDE1006 // Naming Styles

    protected EpObject(EpType type)
    {
        DunderClass = type;
    }

    internal EpObject()
    {
        AllocateSlots(_EpObjectSlotCount);
        DunderClass = null!;
    }

    protected void AllocateSlots(int slotsCount)
    {
        var slots = new object?[slotsCount];
        DunderSlots = slots.AsMemory();
    }

    internal static EpBool DunderEqImplementation(EpObject self, EpObject other) => (EpBool)self.Equals(other);

    internal static EpBool DunderNeImplementation(EpObject self, EpObject other) => (EpBool)!self.Equals(other);
}
