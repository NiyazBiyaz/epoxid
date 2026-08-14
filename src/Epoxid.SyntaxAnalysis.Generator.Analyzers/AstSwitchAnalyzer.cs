using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Epoxid.SyntaxAnalysis.Generator.Analyzers;

// Ok.
#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
public class AstSwitchAnalyzer : DiagnosticAnalyzer
{
    private const string wild_union_name = "Epoxid.SyntaxAnalysis.WildUnionAttribute";
    private const string base_rule_name = "Epoxid.SyntaxAnalysis.BaseRuleAttribute";

    internal const string PGNT001 = "PGNT001";

    private static readonly DiagnosticDescriptor unmatched_ast_components = new(
#pragma warning disable RS2008 // Enable analyzer release tracking // Ah?
        id: PGNT001,
#pragma warning restore RS2008 // Enable analyzer release tracking
        title: "Unmatched AST component types",
        messageFormat: "Unmatched AST component types ({0})",
        category: "Code Quality",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = ImmutableArray.Create(unmatched_ast_components);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => supportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(analyzeStatement, SyntaxKind.SwitchStatement);
        context.RegisterSyntaxNodeAction(analyzeExpression, SyntaxKind.SwitchExpression);
    }

    private void analyzeExpression(SyntaxNodeAnalysisContext context)
    {
        var switchExpression = (SwitchExpressionSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(switchExpression.GoverningExpression, context.CancellationToken);
        var typeSymbol = typeInfo.Type;

        if (typeSymbol == null)
            return;

        var switchableAstTrees = computeSwitchableAstTree(typeSymbol);

        if (!switchableAstTrees.Any())
            return;

        var switchableAst = new SwitchableAst(typeSymbol, switchableAstTrees);

        var cases = switchExpression
            .Arms
            .Where(a => a.WhenClause == null)
            .Select(a => a.Pattern)
            .Where(p => p is ConstantPatternSyntax or DeclarationPatternSyntax)
            .Select(p => p switch
            {
                DeclarationPatternSyntax d => d.Type,
                ConstantPatternSyntax c => c.Expression,
                _ => throw new InvalidOperationException("Unreachable condition."),
            });

        if (!cases.Any())
            return;

        analyzeCore(context, switchExpression, cases, switchableAst);
    }

    private static void analyzeStatement(SyntaxNodeAnalysisContext context)
    {
        var switchStatement = (SwitchStatementSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(switchStatement.Expression, context.CancellationToken);
        var typeSymbol = typeInfo.Type;

        if (typeSymbol == null)
            return;

        var switchableAstTrees = computeSwitchableAstTree(typeSymbol);

        if (!switchableAstTrees.Any())
            return;

        var switchableAst = new SwitchableAst(typeSymbol, switchableAstTrees);

        var declarationCases = switchStatement
            .Sections
            .SelectMany(s => s.Labels)
            .OfType<CasePatternSwitchLabelSyntax>()
            .Where(l => l.WhenClause == null)
            .Select(l => l.Pattern)
            .OfType<DeclarationPatternSyntax>()
            .Select(p => p.Type);

        var constTypeCases = switchStatement
            .Sections
            .SelectMany(s => s.Labels)
            .OfType<CaseSwitchLabelSyntax>()
            .Select(l => l.Value);

        var cases = declarationCases.Concat(constTypeCases);

        if (!cases.Any())
            return;

        analyzeCore(context, switchStatement, cases, switchableAst);
    }

    private static void analyzeCore(
        SyntaxNodeAnalysisContext context,
        SyntaxNode switchNode,
        IEnumerable<ExpressionSyntax> cases,
        SwitchableAst switchableAst)
    {
        var coveredTypes = new List<ITypeSymbol>();
        foreach (var c in cases)
        {
            var type = context.SemanticModel.GetTypeInfo(c);

            if (type.Type == null)
                continue;

            coveredTypes.Add(type.Type);
        }

        var uncovered = switchableAst.GetUncovered(coveredTypes);

        if (uncovered.Count == 0)
            return;

        var uncoveredNames = uncovered.Select(t => t.ToMinimalDisplayString(context.SemanticModel, context.Node.SpanStart));

        var diagnostic = Diagnostic.Create(
            unmatched_ast_components,
            switchNode.GetLocation(),
            properties: uncoveredNames.ToImmutableDictionary(un => un)!,
            string.Join(", ", uncoveredNames));

        context.ReportDiagnostic(diagnostic);
    }

    private static IEnumerable<SwitchableAst> computeSwitchableAstTree(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsSealed)
            // Sealed classes can't be used for switch on types, so skip
            yield break;

        var attributes = typeSymbol.GetAttributes();
        var attribute = attributes.FirstOrDefault(
            attr => attr.AttributeClass?.ToDisplayString() == wild_union_name
                 || attr.AttributeClass?.ToDisplayString() == base_rule_name);

        if (attribute == null)
            yield break;

        foreach (var constant in attribute.ConstructorArguments)
        {
            if (constant.Kind == TypedConstantKind.Array && constant.Values is ImmutableArray<TypedConstant> typedConstants)
            {
                foreach (var typed in typedConstants)
                {
                    if (typed.Kind == TypedConstantKind.Type && typed.Value is ITypeSymbol type)
                        yield return new SwitchableAst(type, computeSwitchableAstTree(type));
                }
            }
        }
    }
}
