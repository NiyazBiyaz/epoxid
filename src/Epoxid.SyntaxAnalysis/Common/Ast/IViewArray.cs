namespace Epoxid.SyntaxAnalysis.Common.Ast;

public interface IViewArray<out TView> : IRedView, IReadOnlyList<TView>
    where TView : IRedView;
