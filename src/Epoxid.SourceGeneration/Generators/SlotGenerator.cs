using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Epoxid.SourceGeneration.Generators;

[Generator]
public class SlotGenerator : BaseGenerator<SlotGenerator.TargetData>
{
    protected override string AttributeMetadataName => "Epoxid.SourceGeneration.EpoxidTypeAttribute";

    private const string readonly_list_of_descriptors = $"global::System.Collections.Generic.IReadOnlyList<{known_descriptor}>";
    private const string known_descriptor = "global::Epoxid.Runtime.Objects.KnownSlotDescriptor";
    private const string instance_name = "_instance";
    private const string value_name = "_value";

    protected override TargetData? CreateTarget(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not ITypeSymbol target)
            return null;

        if (target.BaseType == null)
            return null;

        var slots = ImmutableArray.CreateBuilder<SlotData>();
        foreach (var member in target.GetMembers().OfType<IPropertySymbol>())
        {
            var slotAttribute = member.GetAttributes().FirstOrDefault(SyntaxHelpers.IsSlotAttribute);
            if (slotAttribute is not AttributeData attr)
                continue;

            string propertyName = member.Name;
            string descriptorName = (string)attr.ConstructorArguments[0].Value!;
            string typeName = SyntaxHelpers.GetGlobalQualifiedName(member.Type);
            bool isEpoxidType = SyntaxHelpers.IsEpoxidObject(member.Type);

            bool needNullable = member.Type.NullableAnnotation == NullableAnnotation.Annotated && !member.Type.IsValueType;

            slots.Add(new(
                propertyName,
                descriptorName,
                typeName,
                isEpoxidType,
                needNullable));
        }

        return new TargetData(
            target.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            target.Name,
            new ValueArray<SlotData>(slots.ToImmutable()));
    }

    protected override string Generate(SourceBuilder builder, TargetData target)
    {
        builder.AddLine($"namespace {target.Namespace};");
        builder.BlankLine();
        builder.AddLine($"partial class {target.ClassName}");

        using (builder.BlockScope())
        {
            builder.AddLines($"""
            {GeneratedCodeAttribute}
            private static readonly {readonly_list_of_descriptors} known_slots =
            [
            """);
            using (builder.BlockIndented("];"))
            {
                bool blank = false;
                foreach (var slot in target.Slots)
                {
                    if (blank)
                        builder.BlankLine();
                    blank = true;

                    builder.AddLines($"""
                    new {known_descriptor}()
                    """);
                    using (builder.BlockScope(","))
                    {
                        if (!slot.IsEpoxidType)
                        {
                            builder.BeginLine();
                            builder.Add($"GetSlot = static (_, {instance_name}, _) => ");
                            builder.Add($"new {wrapper(slot.TypeString)}(");
                            builder.Add($"{cast(instance_name, target.ClassName)}.{slot.PropertyName}),");
                            builder.EndLine();

                            builder.BeginLine();
                            builder.Add($"SetSlot = static (_, {instance_name}, {value_name}) => ");
                            builder.Add(cast(instance_name, wrapper(slot.TypeString)));
                            builder.Add($".Value = ");
                            builder.Add(cast(value_name, wrapper(slot.TypeString)));
                            builder.Add(".Value,");
                            builder.EndLine();
                        }
                        else
                        {
                            builder.AddLines($"""
                            GetSlot = static (_, {instance_name}, _) => {cast(instance_name, target.ClassName)}.{slot.PropertyName},
                            SetSlot = static (_, {instance_name}, {value_name}) => {cast(instance_name, target.ClassName)}.{slot.PropertyName} = {cast(value_name, slot.TypeName)},
                            """);
                        }

                        builder.AddLine($"DescriptorName = \"{slot.DescriptorName}\",");
                    }
                }
            }
        }

        return target.Identifier;
    }

    private static string cast(string variable, string type) => $"(({type}){variable})";

    private static string wrapper(string typeName) => $"global::Epoxid.Runtime.Objects.WrapperObject<{typeName}>";

    public record TargetData(string Namespace, string ClassName, ValueArray<SlotData> Slots)
    {
        public string Identifier => $"g_{Namespace}+{ClassName}.g.cs";
    }

    public record SlotData(
        string PropertyName,
        string DescriptorName,
        string TypeName,
        bool IsEpoxidType,
        bool NeedNullableAnnotation)
    {
        public string TypeString => TypeName + (NeedNullableAnnotation ? '?' : "");
    }
}
