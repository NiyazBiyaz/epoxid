namespace PySharp.SyntaxAnalysis;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BaseRuleAttribute(params Type[] inheritors) : Attribute
{
    public readonly Type[] Inheritors = inheritors;
}
