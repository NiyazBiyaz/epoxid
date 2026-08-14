namespace Epoxid.SyntaxAnalysis.Common.Ast;

public interface IRedView
{
    int FullPosition { get; }
    int EndPosition { get; }

    int Position { get; }

    IRedView? Parent { get; }

    SyntaxViewTree SyntaxTree { get; }

    Position2D StartLocation { get; }
    Position2D FullLocation { get; }
    Position2D EndLocation { get; }

    bool IsArray { get; }

    string PrettyPrint();
    string RecoverText();
}
