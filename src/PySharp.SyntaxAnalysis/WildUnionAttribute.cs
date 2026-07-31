namespace PySharp.SyntaxAnalysis;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class WildUnionAttribute(params Type[] members) : Attribute
{
    public readonly Type[] Members = members;
}
