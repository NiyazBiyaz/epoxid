using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PySharp.SyntaxAnalysis.Generator.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AstSwitchFixer))]
[Shared]
public class AstSwitchFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create([AstSwitchAnalyzer.PGNT001]);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var span = diagnostic.Location.SourceSpan;

        var switchNode = root.FindNode(span);

        if (switchNode is SwitchStatementSyntax statement)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add unmatched AST components to statement",
                    cancellationToken => statementAddUnmatchedAst(statement, context.Document, diagnostic.Properties, cancellationToken),
                    equivalenceKey: "AddUnmatchedAstComponentsStatement"),
                diagnostic);
        }
        else if (switchNode is SwitchExpressionSyntax expression)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add unmatched AST components to expression",
                    cancellationToken => expressionAddUnmatchedAst(expression, context.Document, diagnostic.Properties, cancellationToken),
                    equivalenceKey: "AddUnmatchedAstComponentsExpression"),
                diagnostic);
        }
    }

    public sealed override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    private static async Task<Document> expressionAddUnmatchedAst(
        SwitchExpressionSyntax expression,
        Document document,
        ImmutableDictionary<string, string?> unmatchedAsts,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
            return document;

        var astCases = new SyntaxList<SwitchExpressionArmSyntax>(unmatchedAsts
            .Keys
            .Select(astName =>
                SyntaxFactory.SwitchExpressionArm(
                    SyntaxFactory.DeclarationPattern(
                        SyntaxFactory.IdentifierName(astName),
                        SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier("view"))
                    ),
                    SyntaxFactory.ThrowExpression(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("NotImplementedException"),
                            SyntaxFactory.ArgumentList(),
                            null
                        )
                    )
                )
                .WithLeadingTrivia(SyntaxFactory.LineFeed)
            )
        );

        SeparatedSyntaxList<SwitchExpressionArmSyntax> arms;
        if (expression.Arms.FirstOrDefault(a => a.Pattern is DiscardPatternSyntax) is SwitchExpressionArmSyntax withDiscard)
        {
            int index = expression.Arms.IndexOf(withDiscard);
            arms = expression.Arms.InsertRange(index, astCases);
        }
        else
        {
            arms = expression.Arms
                .AddRange(astCases)
                .Add(
                    SyntaxFactory.SwitchExpressionArm(
                        SyntaxFactory.DiscardPattern(),
                        SyntaxFactory.ThrowExpression(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName("NotImplementedException"),
                                SyntaxFactory.ArgumentList(),
                                null
                            )
                        )
                    )
                    .WithLeadingTrivia(SyntaxFactory.LineFeed)
                );
        }

        var newExpression = expression.WithArms(arms);

        root = root.ReplaceNode(expression, newExpression);

        return document.WithSyntaxRoot(root);
    }

    private static async Task<Document> statementAddUnmatchedAst(
        SwitchStatementSyntax statement,
        Document document,
        ImmutableDictionary<string, string?> unmatchedAsts,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
            return document;

        var astCases = new SyntaxList<SwitchLabelSyntax>(unmatchedAsts
            .Keys
            .Select(astName =>
                SyntaxFactory.CasePatternSwitchLabel(
                    SyntaxFactory.ConstantPattern(
                        SyntaxFactory.IdentifierName(astName)
                    ),
                    SyntaxFactory.Token(SyntaxKind.ColonToken)
                )
            )
        );

        var defaultSection = statement.Sections
            .FirstOrDefault(s => s.Labels.Any(l => l is DefaultSwitchLabelSyntax));

        bool wasDefault = defaultSection != null;

        var trivia = defaultSection?.GetLeadingTrivia();

        var oldDefault = defaultSection;

        defaultSection ??=
            SyntaxFactory.SwitchSection(
                SyntaxFactory.List<SwitchLabelSyntax>([
                    SyntaxFactory.DefaultSwitchLabel()
                ]),
                SyntaxFactory.List<StatementSyntax>([
                    SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("NotImplementedException"),
                            SyntaxFactory.ArgumentList(),
                            null
                        )
                    ),
                ])
            )
            .WithLeadingTrivia(SyntaxFactory.LineFeed);

        trivia ??= SyntaxFactory.TriviaList(SyntaxFactory.LineFeed);

        var labels = astCases.Concat(defaultSection.Labels);

        defaultSection = defaultSection
            .WithLabels(new SyntaxList<SwitchLabelSyntax>(labels))
            .WithLeadingTrivia(trivia);

        SwitchStatementSyntax newStatement;
        if (wasDefault)
        {
            newStatement = statement.ReplaceNode(oldDefault!, defaultSection);
        }
        else
        {
            var sections = new SyntaxList<SwitchSectionSyntax>(statement.Sections.Append(defaultSection));
            newStatement = statement.WithSections(sections);
        }

        root = root.ReplaceNode(statement, newStatement);

        return document.WithSyntaxRoot(root);
    }
}
