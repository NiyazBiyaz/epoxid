using System.Collections.Immutable;
// using System.Text;
using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Text;

namespace Epoxid.SourceGeneration.Generators;

[Generator]
public class SlotGenerator : BaseGenerator<SlotGenerator.TargetData>
{
    protected override string AttributeMetadataName => "Epoxid.SourceGeneration.EpoxidTypeAttribute";

    private const string slot_offset = "global::Epoxid.Runtime.SlotOffset";
    private const string readonly_dictionary_string_offset = $"global::System.Collections.ObjectModel.ReadOnlyDictionary<string, {slot_offset}>";
    private const string dictionary_string_offset = $"global::System.Collections.Generic.Dictionary<string, {slot_offset}>";
    private const string collection_extensions_as_readonly = "global::System.Collections.Generic.CollectionExtensions.AsReadOnly";

    protected override TargetData? CreateTarget(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not ITypeSymbol target)
            return null;

        if (target.BaseType == null)
            return null;

        var slots = ImmutableArray.CreateBuilder<SlotData>();
        foreach (var member in target.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.GetMethod == null || member.SetMethod == null)
                continue;

            var slotAttribute = member.GetAttributes().FirstOrDefault(SyntaxHelpers.IsSlotAttribute);
            if (slotAttribute is not AttributeData attr)
                continue;

            string propertyName = member.Name;
            string descriptorName = (string)attr.ConstructorArguments[0].Value!;
            string typeName = SyntaxHelpers.GetGlobalQualifiedName(member.Type);

            slots.Add(new(
                propertyName,
                descriptorName,
                typeName,
                member.Type.NullableAnnotation == NullableAnnotation.Annotated,
                member.Type.IsValueType,
                member.DeclaredAccessibility,
                member.GetMethod.DeclaredAccessibility,
                member.SetMethod.DeclaredAccessibility));
        }

        return new TargetData(
            target.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            target.Name,
            target.BaseType.Name,
            new ValueArray<SlotData>(slots.ToImmutable())
        );
    }

    protected override string Generate(SourceBuilder builder, TargetData target)
    {
        builder.AddLine($"namespace {target.Namespace};");

        builder.AddLine($"partial class {target.ClassName}");

        using (builder.BlockScope())
        {
            builder.AddLines($"""
            // Slots count of '{target.ClassName}' type.
            {GeneratedCodeAttribute}
            protected const int {classSlotCount(target.ClassName)} = {classSlotCount(target.BaseClassName)} + {target.Slots.Length};
            """);
            builder.BlankLine();

            builder.AddLines($"""
            {GeneratedCodeAttribute}
            internal override int SlotsCount => {classSlotCount(target.ClassName)};
            """);
            builder.BlankLine();

            builder.AddLines($"""
            {GeneratedCodeAttribute}
            private static readonly {readonly_dictionary_string_offset} slot_descriptors_mapping
                = {collection_extensions_as_readonly}(
                new {dictionary_string_offset}
            """);
            using (builder.BlockScope(");"))
            {
                foreach (var slot in target.Slots)
                {
                    builder.AddLine($"[\"{slot.DescriptorName}\"] = new {slot_offset}({slotIndex(slot.PropertyName)}),");
                }
            }

            for (int i = 0; i < target.Slots.Length; i++)
            {
                builder.BlankLine();
                var slot = target.Slots[i];

                string accessibility = GetAccessibilityString(slot.PropertyAccessibility);
                string getAccess = GetAccessibilityString(slot.GetAccessibility);
                string setAccess = GetAccessibilityString(slot.SetAccessibility);

                builder.AddLines($"""
                // Property of the '{slot.DescriptorName}' slot
                {accessibility} partial {slot.TypeString} {slot.PropertyName}
                """);

                using (builder.BlockScope())
                {
                    builder.AddLine(GeneratedCodeAttribute);
                    builder.BeginLine();
                    if (slot.GetAccessibility < slot.PropertyAccessibility)
                    {
                        builder.Add($"{getAccess} ");
                    }
                    builder.Add("get");
                    builder.EndLine();

                    using (builder.BlockScope())
                    {
                        builder.AddLines($"""
                        {slotsBoundCheck(slot.PropertyName)}
                        var _span = base.DunderSlots.Span;
                        """);

                        if (!slot.IsTypeNullable)
                        {
                            builder.AddLines($"""
                            var _value = _span[{slotIndex(slot.PropertyName)}];
                            if (_value == null)
                                throw new global::System.NullReferenceException("'{slot.PropertyName}' slot value is null.");
                            return ({slot.TypeString})_value;
                            """);
                        }
                        else
                        {
                            builder.AddLine($"return ({slot.TypeString})_span[{slotIndex(slot.PropertyName)}];");
                        }
                    }

                    builder.AddLine(GeneratedCodeAttribute);
                    builder.BeginLine();
                    if (slot.SetAccessibility < slot.PropertyAccessibility)
                    {
                        builder.Add($"{setAccess} ");
                    }
                    builder.Add("set");
                    builder.EndLine();

                    using (builder.BlockScope())
                    {
                        builder.AddLines($"""
                        {slotsBoundCheck(slot.PropertyName)}

                        var _span = base.DunderSlots.Span;
                        _span[{slotIndex(slot.PropertyName)}] = value;
                        """);
                    }
                }
                builder.BlankLine();

                builder.AddLines($"""
                // Index of the slot '{slot.DescriptorName}'
                {GeneratedCodeAttribute}
                private const int {slotIndex(slot.PropertyName)} = {classSlotCount(target.BaseClassName)} + {i};
                """);
            }
        }

        return target.Identifier;
    }

    private static string slotsBoundCheck(string propertyName)
    {
        return $"""
        if (base.DunderSlots.Length <= {slotIndex(propertyName)})
            throw new global::System.Diagnostics.UnreachableException("Slot '{propertyName}' access error: slots have less size then slot index.");
        """;
    }

    private static string slotIndex(string propertyName) => $"_{propertyName}_Index";

    private static string classSlotCount(string className) => $"_{className}SlotCount";

    public record TargetData(
        string Namespace,
        string ClassName,
        string BaseClassName,
        ValueArray<SlotData> Slots)
    {
        public string Identifier => $"{Namespace}+{ClassName}_{Slots.Length}.g.cs";
    }

    public record SlotData(
        string PropertyName,
        string DescriptorName,
        string TypeName,
        bool IsTypeNullable,
        bool IsValueType,
        Accessibility PropertyAccessibility,
        Accessibility GetAccessibility,
        Accessibility SetAccessibility)
    {
        public string TypeString => TypeName + (IsTypeNullable && !IsValueType ? "?" : "");
    }
}
