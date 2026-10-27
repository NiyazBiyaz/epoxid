using Epoxid.SyntaxAnalysis.Common;
using Epoxid.SyntaxAnalysis.Tokens;

namespace Epoxid.SyntaxAnalysis;

public partial class PythonParser
{
    public static FileView ParseModuleView(ReadOnlyMemory<char> content)
    {
        var mainSyncPoint = SynchronizationPoint.ClearPoint(new MemoryCharBuffer(content));
        var tokenizer = new Tokenizer(mainSyncPoint);
        var tokensStream = new TokenNodeStream(tokenizer);

        var parser = new PythonParser(tokensStream);

        var fileNode = parser.Parse() ?? throw new ArgumentException("Given content cannot be parsed");

        var fileView = fileNode.GetView(0, null);
        fileView.SyntaxTree = new SyntaxViewTree
        {
            Root = fileView,
            PositionMap = tokenizer.PositionMap,
        };

        return fileView;
    }

    public static FileView ParseModuleView(string content) => ParseModuleView(content.AsMemory());
}
