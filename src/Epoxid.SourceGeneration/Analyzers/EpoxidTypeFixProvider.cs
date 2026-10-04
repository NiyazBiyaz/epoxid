using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Epoxid.SourceGeneration.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EpoxidTypeCodeFixProvider))]
public class EpoxidTypeCodeFixProvider : CodeFixProvider
{
    private readonly static ImmutableArray<string> diagnostics = ImmutableArray.Create(
        Rules.AddBaseInitializer.Id
    );

    public override ImmutableArray<string> FixableDiagnosticIds => diagnostics;

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var span = diagnostic.Location.SourceSpan;

        var node = root.FindNode(span);

        if (node is not ConstructorDeclarationSyntax ctor)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add 'base' initializer",
                ct => createChangedSolution(context.Document, ctor, ct),
                equivalenceKey: "AddBaseInitializer"),
            diagnostic);
    }

    private static async Task<Document> createChangedSolution(Document document, ConstructorDeclarationSyntax ctor, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct);
        if (root == null)
            return document;

        var newCtor = ctor
            .WithInitializer(
                SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    SyntaxFactory.Token(SyntaxKind.ColonToken),
                    SyntaxFactory.Token(SyntaxKind.BaseKeyword),
                    SyntaxFactory.ArgumentList())
                        .WithLeadingTrivia(
                            SyntaxFactory.LineFeed,
                            SyntaxFactory.SyntaxTrivia(
                                SyntaxKind.WhitespaceTrivia, "        ")))
            .WithParameterList(
                ctor.ParameterList.WithTrailingTrivia());

        root = root.ReplaceNode(ctor, newCtor);

        return document.WithSyntaxRoot(root);
    }
}
