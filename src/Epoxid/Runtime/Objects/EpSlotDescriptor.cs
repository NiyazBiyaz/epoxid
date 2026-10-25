namespace Epoxid.Runtime.Objects;

public class EpSlotDescriptor(SlotOffset offset, string slotName) : EpObject
{
    private readonly SlotOffset offset = offset;
    private readonly string slotName = slotName;

    public static readonly EpType SlotDescriptor = new("slot descriptor", [Object], EpType.Type)
    {
        DunderGet = dunderGetImplementation,
        DunderSet = dunderSetImplementation,
    };

    private static EpObject dunderGetImplementation(EpObject self, EpObject instance, EpObject owner)
    {
        if (self is not EpSlotDescriptor descriptor)
            throw new ArgumentException($"Fatal error: {nameof(self)} is not a descriptor object.");

        if (descriptor.offset.Value >= instance.SlotsCount)
            throw new InvalidOperationException($"Trying to use slot descriptor '{descriptor.slotName}' on object that does not have enough slots.");

        var slots = instance.DunderSlots.Span;

        if (slots[descriptor.offset.Value] is not EpObject slotValue)
        {
            throw new NotImplementedException("CLR objects wrapper is not implemented yet.");
        }

        return slotValue;
    }

    private static void dunderSetImplementation(EpObject self, EpObject instance, EpObject value)
    {
        if (self is not EpSlotDescriptor descriptor)
            throw new ArgumentException($"Fatal error: {nameof(self)} is not a descriptor object.");

        if (descriptor.offset.Value >= instance.SlotsCount)
            throw new InvalidOperationException($"Trying to use slot descriptor '{descriptor.slotName}' on object that does not have enough slots.");

        var slots = instance.DunderSlots.Span;

        slots[descriptor.offset.Value] = value;
    }

    public override string ToString() => $"<slot descriptor of '{slotName}'>";
}
