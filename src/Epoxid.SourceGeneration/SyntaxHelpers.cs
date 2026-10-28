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

    public static bool IsBuiltinFunctionAttribute(AttributeData? attribute)
        => IsBuiltinFunctionAttribute(attribute?.AttributeClass);

    public static bool IsBuiltinFunctionAttribute(ITypeSymbol? type)
        => type != null && GetQualifiedName(type) == "Epoxid.SourceGeneration.BuiltinFunctionAttribute";

    public static string DisplayNamespace(INamespaceSymbol namespaceSymbol)
        => namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

    public static string GetQualifiedName(ISymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    public static string GetGlobalQualifiedName(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string GetTypeString(ITypeSymbol type)
    {
        bool needNullable = type.NullableAnnotation == NullableAnnotation.Annotated && !type.IsValueType;
        return GetGlobalQualifiedName(type) + (needNullable ? '?' : "");
    }
}
