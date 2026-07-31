using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace PySharp.SyntaxAnalysis.Generator.Analyzers;

public sealed class SwitchableAstNode(ITypeSymbol type, IEnumerable<SwitchableAstNode> children)
{
    private readonly ITypeSymbol type = type;
    private readonly ImmutableArray<SwitchableAstNode> children = children.ToImmutableArray();

    private IEnumerable<ITypeSymbol> childrenTypes => children.Select(c => c.type);

    internal List<ITypeSymbol> GetUncovered(IEnumerable<ITypeSymbol> covered)
    {
        if (childrenTypes.All(c => covered.Contains(c, SymbolEqualityComparer.Default)))
            return [];

        List<ITypeSymbol> uncoveredChildren = [];

        foreach (var child in children)
        {
            if (child.hasCoveredChildren(covered))
            {
                uncoveredChildren.AddRange(child.GetUncovered(covered));
            }
            else if (!covered.Contains(child.type, SymbolEqualityComparer.Default))
            {
                uncoveredChildren.Add(child.type);
            }
        }

        return uncoveredChildren;
    }

    private bool hasCoveredChildren(IEnumerable<ITypeSymbol> covered)
        => childrenTypes.Any(c => covered.Contains(c, SymbolEqualityComparer.Default));
}
