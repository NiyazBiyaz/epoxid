using Epoxid.SyntaxAnalysis.Common.Ast;

namespace Epoxid.SyntaxAnalysis.Common;

public interface ITokenNodeStream
{
    int Index { get; set; }

    TokenNode GetAndAdvance();
    TokenNode PeekToken();

#if PARSER_VERBOSE
    TokenNode? PeekOrDefault();
#endif
}
