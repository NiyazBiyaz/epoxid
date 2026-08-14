using System.Diagnostics.CodeAnalysis;

namespace Epoxid.SyntaxAnalysis.Tokens;

public interface ITokenizer
{
    bool ShouldStop { get; }
    TokenizerError Error { get; }
    string? ErrorMessage { get; }

    SynchronizationPoint Synchronize();
    void ReadNext([NotNull] out Token? token);
}
