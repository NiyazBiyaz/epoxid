using Microsoft.CodeAnalysis;

namespace Epoxid.SourceGeneration;

public static class SyntaxHelpers
{
    public static bool IsEpoxidTypeAttribute(AttributeData? attribute)
        => IsEpoxidTypeAttribute(attribute?.AttributeClass);

    public static bool IsEpoxidTypeAttribute(ITypeSymbol? type)
        => type != null && GetQualifiedName(type) == "Epoxid.SourceGeneration.EpoxidTypeAttribute";

    public static bool IsEpoxidObject(ITypeSymbol? type)
        => type != null && (
            GetQualifiedName(type) == "Epoxid.Runtime.Objects.EpObject" ||
            IsEpoxidObject(type.BaseType));

    public static bool IsSlotAttribute(AttributeData? attribute)
        => IsSlotAttribute(attribute?.AttributeClass);

    public static bool IsSlotAttribute(ITypeSymbol? type)
        => type != null && GetQualifiedName(type) == "Epoxid.SourceGeneration.SlotAttribute";

    public static string GetQualifiedName(ISymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    public static string GetGlobalQualifiedName(ISymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
