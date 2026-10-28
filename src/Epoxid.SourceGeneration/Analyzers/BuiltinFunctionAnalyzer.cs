using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Epoxid.SourceGeneration.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BuiltinFunctionAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> diagnostics = ImmutableArray.Create(
    [
        Rules.MakePartial,
        Rules.MakeStatic,
    ]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => diagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(analyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void analyzeMethod(SyntaxNodeAnalysisContext ctx)
    {
        var methodSyntax = (MethodDeclarationSyntax)ctx.Node;
        if (methodSyntax.Parent is not ClassDeclarationSyntax classSyntax)
            return;

        bool haveStatic = methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);
        bool haveAttribute = ctx.SemanticModel
            .GetDeclaredSymbol(methodSyntax, ctx.CancellationToken)
            ?.GetAttributes()
            .Any(SyntaxHelpers.IsBuiltinFunctionAttribute)
            ?? false;

        if (!haveStatic && haveAttribute)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Rules.MakeStatic,
                methodSyntax.Identifier.GetLocation()));
        }

        bool havePartial = classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

        if (!havePartial && haveAttribute)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Rules.MakePartial,
                classSyntax.Identifier.GetLocation()));
        }
    }
}
