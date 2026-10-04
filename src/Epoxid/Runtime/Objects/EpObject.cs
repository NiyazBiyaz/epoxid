namespace Epoxid.Runtime.Objects;

public partial class EpObject
{
    public EpType DunderClass { get; internal set; }

    private protected Memory<object?> DunderSlots { get; set; }

    private protected virtual int SlotsCount { get; } = _EpObjectSlotCount;

    public EpDict? DunderDict
    {
        get => (EpDict?)DunderSlots.Span[dunder_dict_slot_index];
        set => DunderSlots.Span[dunder_dict_slot_index] = value;
    }

    private const int dunder_dict_slot_index = 0;

#pragma warning disable IDE1006 // Naming Styles

    protected const int _EpObjectSlotCount = 1;

#pragma warning restore IDE1006 // Naming Styles

    protected EpObject(EpType type)
    {
        allocateSlots();
        DunderClass = type;
    }

    internal EpObject()
    {
        allocateSlots();
        DunderClass = null!;
    }

    private void allocateSlots()
    {
        var slots = new object?[SlotsCount];
        DunderSlots = slots.AsMemory();
    }

    internal static EpObject DunderNewImplementation(EpObject type) => new((EpType)type);

    internal static EpBool DunderEqImplementation(EpObject self, EpObject other) => (EpBool)self.Equals(other);

    internal static EpBool DunderNeImplementation(EpObject self, EpObject other) => (EpBool)!self.Equals(other);
}
