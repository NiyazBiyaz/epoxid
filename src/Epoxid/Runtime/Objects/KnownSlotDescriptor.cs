namespace Epoxid.Runtime.Objects;

public class KnownSlotDescriptor
{
    public required DescriptorGetFunction GetSlot { get; init; }
    public required DescriptorSetFunction SetSlot { get; init; }
    public required string DescriptorName { get; init; }
}
