using Epoxid.SyntaxAnalysis.Tokens;
using Epoxid.SyntaxAnalysis.Common.Ast;
using System.Diagnostics;

namespace Epoxid.SyntaxAnalysis.Common;

public class TokenNodeStream(ITokenizer tokenizer) : ITokenNodeStream
{
    private readonly List<TokenNode> tokens = [];
    private readonly ITokenizer tokenizer = tokenizer;

    private TokenType lastTokenType;

    private bool virtualNewLineInserted = false;

    public int Index
    {
        get;
        set
        {
            if (value == field)
                return;

            Debug.Assert(value >= 0 && value <= tokens.Count, $"value={value} tokens.Count={tokens.Count}");

            field = value;
        }
    } = 0;

    public TokenNode GetAndAdvance()
    {
        var tok = PeekToken();
        Index += 1;
        return tok;
    }

    public TokenNode PeekToken()
    {
        if (Index == tokens.Count)
        {
            List<TokenNode> trivias = [];
            TokenNode node;
            do
            {
                Debug.Assert(!tokenizer.ShouldStop, $"Tokens count: {tokens.Count}, Index: {Index}");

                tokenizer.ReadNext(out var token);
                var tok = token.Value;
                if (tok.Type.IsTrivia)
                {
                    node = new(tok, []);
                    trivias.Add(node);
                }
                else if (tok.Type.IsError)
                {
                    Debug.Assert(tokenizer.Error != TokenizerError.NoError);

                    node = new InvalidTokenNode(tok, [], tokenizer.ErrorMessage, tokenizer.Error);
                    trivias.Add(node);
                }
                else
                {
                    node = new(tok, trivias);
                }
            }
            while (node.Type.IsTrivia || node.Type.IsError);

            // If EOF reached and last token is not NewLine, add it to be able parse imperfect code
            if (tokenizer.EofReached)
            {
                bool needVirtualNewLine = lastTokenType != TokenType.NewLine
                    && lastTokenType != TokenType.Dedent
                    && !virtualNewLineInserted;
                if (needVirtualNewLine)
                {
                    virtualNewLineInserted = true;
                    var virtualToken = new Token(TokenType.NewLine, "");
                    var virtualNode = new TokenNode(virtualToken, []);
                    tokens.Add(virtualNode);
                }
            }

            lastTokenType = node.Type;

            tokens.Add(node);
        }

        return tokens[Index];
    }

#if PARSER_VERBOSE
    public TokenNode? PeekOrDefault()
    {
        if (Index == tokens.Count)
        {
            return null;
        }

        return tokens[Index];
    }
#endif
}
