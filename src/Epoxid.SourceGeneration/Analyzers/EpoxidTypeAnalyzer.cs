using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Epoxid.SourceGeneration.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EpoxidTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> supported_diagnostics = ImmutableArray.Create([
        Rules.MakePartial,
        Rules.MakeClassDerivedFromEpObject,
        Rules.AddBaseInitializer,
    ]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => supported_diagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(analyzeClass, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(analyzeConstructors, SyntaxKind.ClassDeclaration);
    }

    private void analyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;

        analyzeNeedToBeDerived(context, classSyntax);

        if (classSyntax.Ancestors().OfType<ClassDeclarationSyntax>().Any())
            return;

        analyzeNeedPartial(context, classSyntax);
    }

    private static bool analyzeNeedPartial(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax node)
    {
        bool requiresPartial = false;

        foreach (var nested in node.DescendantNodes().OfType<ClassDeclarationSyntax>())
            requiresPartial |= analyzeNeedPartial(context, nested);

        if (!requiresPartial)
            requiresPartial = context.SemanticModel
                .GetDeclaredSymbol(node)
                ?.GetAttributes()
                .Any(SyntaxHelpers.IsEpoxidTypeAttribute) ?? false;

        bool isPartial = node.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword));

        if (requiresPartial && !isPartial)
            context.ReportDiagnostic(Diagnostic.Create(Rules.MakePartial, node.GetLocation()));

        foreach (var prop in node.Members.OfType<PropertyDeclarationSyntax>())
            analyzePropertyNeedPartial(context, prop);

        return requiresPartial;
    }

    private static void analyzePropertyNeedPartial(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node)
    {
        if (node.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword)))
            return;

        bool needPartial = context.SemanticModel
            .GetDeclaredSymbol(node)
            ?.GetAttributes()
            .Any(SyntaxHelpers.IsSlotAttribute) ?? false;

        if (needPartial)
            context.ReportDiagnostic(Diagnostic.Create(Rules.MakePartial, node.Identifier.GetLocation()));
    }

    private static void analyzeNeedToBeDerived(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax node)
    {
        bool requiresDerived = context.SemanticModel
            .GetDeclaredSymbol(node)
            ?.GetAttributes()
            .Select(attr => attr.AttributeClass)
            .Any(SyntaxHelpers.IsEpoxidTypeAttribute) ?? false;

        bool isEpoxidObject = SyntaxHelpers.IsEpoxidObject(context.SemanticModel.GetDeclaredSymbol(node));

        if (requiresDerived && !isEpoxidObject)
            context.ReportDiagnostic(Diagnostic.Create(Rules.MakeClassDerivedFromEpObject, node.GetLocation()));
    }

    private static void analyzeConstructors(SyntaxNodeAnalysisContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;

        bool isEpoxidType = context.SemanticModel
            .GetDeclaredSymbol(classSyntax)
            ?.GetAttributes()
            .Any(SyntaxHelpers.IsEpoxidTypeAttribute) ?? false;

        if (!isEpoxidType)
            return;

        foreach (var ctor in classSyntax.Members.OfType<ConstructorDeclarationSyntax>())
        {
            if (ctor.Initializer is not ConstructorInitializerSyntax initializer)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.AddBaseInitializer, ctor.GetLocation(), ctor));
            }
        }
    }
}
