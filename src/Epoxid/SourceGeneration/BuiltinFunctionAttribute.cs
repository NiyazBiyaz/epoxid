namespace Epoxid.SourceGeneration;

[AttributeUsage(AttributeTargets.Method)]
public class BuiltinFunctionAttribute(string publicName, params string[] defaults) : Attribute
{
    public string PublicName => publicName;
    public string[] DefaultValues = defaults;
}
