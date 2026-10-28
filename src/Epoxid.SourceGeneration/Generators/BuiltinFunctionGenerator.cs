using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Epoxid.SourceGeneration.Generators;

[Generator]
public class BuiltinFunctionGenerator : BaseGenerator<BuiltinFunctionGenerator.TargetData>
{
    protected override string AttributeMetadataName => "Epoxid.SourceGeneration.BuiltinFunctionAttribute";

    private const string readonly_span_of_epobject = "global::System.ReadOnlySpan<global::Epoxid.Runtime.Objects.EpObject>";
    private const string variadic_argument_type_string = readonly_span_of_epobject;
    private const string keyword_variadic_argument_type_string = "global::Epoxid.Runtime.Objects.EpDict";
    private const string function_parameter = "global::Epoxid.Runtime.FunctionParameter";
    private const string function_parameters_descr = "global::Epoxid.Runtime.FunctionParametersDescription";

    protected override TargetData? CreateTarget(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not IMethodSymbol method)
            return null;

        if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Private)
            return null;

        var builtinFunctionAttr = method.GetAttributes().FirstOrDefault(SyntaxHelpers.IsBuiltinFunctionAttribute);
        if (builtinFunctionAttr is not AttributeData attr)
            return null;

        string functionName = (string)attr.ConstructorArguments[0].Value!;
        string returnType = SyntaxHelpers.GetGlobalQualifiedName(method.ReturnType);

        // TODO: maybe add '/' support (positional-only parameters)
        bool wasVariadic = false;

        ReadOnlySpan<char> defaultName = "", defaultValue = "";
        string attributeArgument;
        string defaultValueString = "";
        int defaultValueCounter = 0;

        if (attr.ConstructorArguments[1].Values.Length > defaultValueCounter)
        {
            attributeArgument = (string)attr.ConstructorArguments[1].Values[defaultValueCounter++].Value!;
            if (!trySplitDefaultValueString(attributeArgument, out defaultName, out defaultValue))
                return null;
        }

        var parameters = ImmutableArray.CreateBuilder<ParameterData>(initialCapacity: method.Parameters.Length);

        foreach (var param in method.Parameters)
        {
            string paramName = param.Name;
            string typeString = SyntaxHelpers.GetTypeString(param.Type);

            bool isVariadic = typeString == variadic_argument_type_string;
            if (isVariadic && wasVariadic)
            {
                return null;
            }
            bool isKeywordVariadic = typeString == keyword_variadic_argument_type_string && paramName.StartsWith("_");

            if (defaultName.SequenceEqual(paramName))
            {
                defaultValueString = defaultValue.ToString();

                if (attr.ConstructorArguments[1].Values.Length > defaultValueCounter)
                {
                    attributeArgument = (string)attr.ConstructorArguments[1].Values[defaultValueCounter++].Value!;
                    if (!trySplitDefaultValueString(attributeArgument, out defaultName, out defaultValue))
                        return null;
                }
            }

            parameters.Add(new(
                paramName,
                typeString,
                isVariadic,
                isKeywordVariadic,
                false,
                wasVariadic,
                defaultValueString == "" ? null : defaultValueString));

            wasVariadic |= isVariadic;
        }

        Debug.Assert(parameters.Count == method.Parameters.Length);

        return new(
            SyntaxHelpers.DisplayNamespace(method.ContainingNamespace),
            method.ContainingType?.Name ?? "",
            functionName,
            method.Name,
            returnType,
            new(parameters.MoveToImmutable()));
    }

    private static bool trySplitDefaultValueString(ReadOnlySpan<char> str, out ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
    {
        int splitPoint = 0;
        while (str.Length > splitPoint && str[splitPoint] != '=')
            splitPoint++;

        if (str.Length == splitPoint)
        {
            name = value = default;
            return false;
        }

        name = str.Slice(0, splitPoint);

        value = str.Slice(splitPoint + 1);

        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }

    protected override string Generate(SourceBuilder builder, TargetData target)
    {
        builder.AddLine("// For convenience reason, since we accept code from user in default values, need to add this namespace");
        builder.AddLine("using global::Epoxid.Runtime.Objects;");
        builder.BlankLine();
        builder.AddLine($"namespace {target.Namespace};");
        builder.BlankLine();
        builder.AddLine($"partial class {target.ClassName}");

        using var _ = builder.BlockScope();
        // Add function parameters description
        builder.AddLine($"""
        private static readonly {function_parameters_descr} {target.FunctionName}_parameters = new()
        """);
        using (builder.BlockScope(";"))
        {
            if (target.Parameters.FirstOrDefault(p => p.IsVariadic) is ParameterData argsParam)
            {
                builder.AddLine($"VariadicPositionalParam = {function_parameter}.Variadic(\"{argsParam.Name}\"),");
            }

            if (target.Parameters.FirstOrDefault(p => p.IsKeywordVariadic) is ParameterData kwargsParam)
            {
                builder.AddLine($"VariadicKeywordParam = {function_parameter}.Variadic(\"{kwargsParam.Name}\"),");
            }

            if (target.Parameters.Any(p => p.IsPositionalOnly))
            {
                builder.AddLine("PositionalOnlyParams = [");
                using var __ = builder.BlockIndented("],");

                foreach (var param in target.Parameters.Where(p => p.IsPositionalOnly))
                    builder.AddLine(funcParam(param));
            }

            if (target.Parameters.Any(p => p.IsFree))
            {
                builder.AddLine("FreeParams = [");
                using var __ = builder.BlockIndented("],");

                foreach (var param in target.Parameters.Where(p => p.IsFree))
                    builder.AddLine(funcParam(param));
            }

            if (target.Parameters.Any(p => p.IsKeywordOnly))
            {
                builder.AddLine("KeywordOnlyParams = [");
                using var __ = builder.BlockIndented("],");

                foreach (var param in target.Parameters.Where(p => p.IsKeywordOnly))
                    builder.AddLine(funcParam(param));
            }
        }

        builder.BlankLine();

        builder.AddLine(GeneratedCodeAttribute);
        builder.AddLine($"public static {target.ReturnType} {target.FunctionName}({readonly_span_of_epobject} __args)");

        // TODO: calls with keyword arguments
        // Generates call for the positional-only arguments passed.
        using (builder.BlockScope())
        {
            builder.AddLine("int __consumedCount = 0;");
            builder.BlankLine();
            for (int argumentIndex = 0; argumentIndex < target.Parameters.Length && !target.Parameters[argumentIndex].IsKeyword; argumentIndex++)
            {
                var param = target.Parameters[argumentIndex];

                if (!param.IsVariadic)
                {
                    builder.AddLines($$"""
                    {{param.TypeString}} {{param.Name}};
                    if ({{argumentIndex}} < __args.Length)
                    {
                        {{param.Name}} = ({{param.TypeString}})__args[{{argumentIndex}}];
                        __consumedCount += 1;
                    }
                    else
                    """);

                    if (param.HasDefault)
                    {
                        builder.AddLines($$"""
                    {
                        {{param.Name}} = {{param.DefaultValue}};
                    }
                    """);
                    }
                    else
                    {
                        builder.AddLines("""
                    {
                        // TODO: throw errors about argument to the Engine API
                        // Probably redundant check because should be checked through the params description, but anyway.
                        throw new ArgumentException("Given args does not have enough values to unpack.");
                    }
                    """);
                    }
                }
                else
                {
                    builder.AddLine($"var {param.Name} = __args[__consumedCount..];");
                }

                builder.BlankLine();
            }

            foreach (var param in target.Parameters.Where(p => p.IsKeyword))
            {
                builder.AddLine($"{param.TypeString} {param.Name} = {param.DefaultValue};");
            }
            builder.BlankLine();

            builder.BeginLine();
            builder.Add($"return {target.ImplementationName}(");
            bool addComma = false;
            foreach (var param in target.Parameters)
            {
                if (addComma)
                    builder.Add(", ");
                addComma = true;

                builder.Add(param.Name);
            }
            builder.Add(");");
            builder.EndLine();
        }

        return target.Identifier;
    }

    protected override bool IsGeneratorCandidate(SyntaxNode node, CancellationToken ct)
    {
        if (node is not MethodDeclarationSyntax)
            return false;

        return true;
    }

    private static string funcParam(ParameterData param) => $"new(\"{param.Name}\", {param.IsRequiredString}),";

    public record TargetData(
        string Namespace,
        string ClassName,
        string FunctionName,
        string ImplementationName,
        string ReturnType,
        ValueArray<ParameterData> Parameters)
    {
        public string Identifier => $"g_{Namespace}+{ClassName}+{FunctionName}.g.cs";
    }

    public record ParameterData(
        string Name,
        string TypeString,
        bool IsVariadic,
        bool IsKeywordVariadic,
        bool IsPositionalOnly,
        bool IsKeywordOnly,
        string? DefaultValue)
    {
        public bool HasDefault => DefaultValue != null;
        public string IsRequiredString => !HasDefault ? "true" : "false";
        public bool IsFree => !IsKeywordOnly && !IsPositionalOnly && !IsVariadic && !IsKeywordVariadic;
        public bool IsKeyword => IsKeywordOnly || IsKeywordVariadic;
    }
}
