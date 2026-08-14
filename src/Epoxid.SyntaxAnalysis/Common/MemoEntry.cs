using Epoxid.SyntaxAnalysis.Common.Ast;

namespace Epoxid.SyntaxAnalysis.Common;

public readonly record struct MemoEntry<TNode>(int EndPosition, TNode? Cache)
    where TNode : IGreenNode;
