namespace Epoxid.SourceGeneration;

[AttributeUsage(AttributeTargets.Property)]
public class SlotAttribute(string descriptorName) : Attribute
{
    public string DescriptorName { get; } = descriptorName;
}
