using System.Diagnostics;

namespace Epoxid.SyntaxAnalysis.Common.Ast;

public abstract class RedView : IRedView
{
    protected readonly IGreenNode Green;

    public int FullPosition { get; }
    public IRedView? Parent { get; }

    public int EndPosition
    {
        get
        {
            if (field == default)
            {
                field = FullPosition + Green.FullWidth;
            }
            return field;
        }
    }

    public bool IsArray => false;

    private SyntaxViewTree? syntaxTree = null;

    public SyntaxViewTree SyntaxTree
    {
        get
        {
            // Found nearest parent with the syntax tree and cache it.
            syntaxTree ??= Parent?.SyntaxTree
                ?? throw new NullReferenceException("SyntaxTree for the current view tree is not set.");

            return syntaxTree.Value;
        }
        set => syntaxTree = value;
    }

    public Position2D FullLocation => SyntaxTree.PositionMap.GetPosition2D(FullPosition);

    public Position2D StartLocation => SyntaxTree.PositionMap.GetPosition2D(Position);

    public Position2D EndLocation => SyntaxTree.PositionMap.GetPosition2D(EndPosition);

    public int Position => FullPosition + (Green.TriviaWidth ?? 0);

    protected RedView(IGreenNode green, int position, IRedView? parentView)
    {
        Debug.Assert(!parentView?.IsArray ?? true, "Arrays cannot to be used as parent.");

        Green = green;
        Parent = parentView;
        FullPosition = position;
    }

    public IEnumerable<IRedView> Children()
    {
        if (Green.Children == null)
            yield break;

        for (int childIndex = 0; childIndex < Green.Children.Count; childIndex++)
        {
            var child = Green.Children[childIndex];

            if (child.IsArray)
            {
                int widthAccumulator = 0;

                foreach (var childItem in child.Children!)
                {
                    var childView = childItem.GetView(widthAccumulator, this);

                    foreach (var grandChild in childView.ChildrenAndSelf())
                    {
                        yield return grandChild;
                    }

                    widthAccumulator += childItem.FullWidth;
                }
            }
            else
            {
                var childView = child.GetView(childIndex, this);

                foreach (var grandChild in childView.ChildrenAndSelf())
                {
                    yield return grandChild;
                }
            }
        }
    }

    public IEnumerable<IRedView> ChildrenAndSelf()
    {
        yield return this;

        foreach (var child in Children())
        {
            yield return child;
        }
    }

    public int GetPositionFor(int childIndex)
    {
        int position = FullPosition;
        for (int beforeChild = 0; beforeChild < childIndex; beforeChild++)
        {
            position += Green.Children?[beforeChild].FullWidth ?? 0;
        }
        return position;
    }

    public string RecoverText() => Green.RecoverText();

    public string PrettyPrint() => Green.PrettyPrint();
}
