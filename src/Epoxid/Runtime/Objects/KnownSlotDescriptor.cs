namespace Epoxid.Runtime.Objects;

public class KnownSlotDescriptor : EpObject
{
    public required DescriptorGetFunction GetSlot { get; init; }
    public required DescriptorSetFunction SetSlot { get; init; }
    public required string DescriptorName { get; init; }

    public static readonly EpType SlotDescriptor = new(
        "slot descriptor",
        [Object],
        EpType.Type)
    {
        DunderGet = dunderGetImplementation,
        DunderSet = dunderSetImplementation,
    };

    public KnownSlotDescriptor() : base(SlotDescriptor)
    {
    }

    private static EpObject? dunderGetImplementation(EpObject self, EpObject instance, EpObject owner)
    {
        if (self is not KnownSlotDescriptor descriptor)
            throw new ArgumentException($"Not a {nameof(KnownSlotDescriptor)} object");

        return descriptor.GetSlot(self, instance, owner);
    }
    private static void dunderSetImplementation(EpObject self, EpObject instance, EpObject value)
    {
        if (self is not KnownSlotDescriptor descriptor)
            throw new ArgumentException($"Not a {nameof(KnownSlotDescriptor)} object");

        descriptor.SetSlot(self, instance, value);
    }
}
