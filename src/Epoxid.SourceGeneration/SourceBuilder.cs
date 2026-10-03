using System.Text;

namespace Epoxid.SourceGeneration;

public class SourceBuilder
{
    private readonly StringBuilder builder = new();

    private const string indent_string = "    ";
    private const char new_line_char = '\n';

    private int indent = 0;

    public Block BlockScope()
    {
        AddLine("{");
        indent += 1;
        return new Block(this);
    }

    public void Add(string code) => builder.Append(code);

    public void AddLine(string line)
    {
        BeginLine();

        builder.Append(line);

        EndLine();
    }

    public void AddLines(string lines)
    {
        // Since netstandard2.0 doesn't provide any method to enumerate lines,
        // just enumerating whole string.
        // Maybe it is better than creating substrings via .Split()
        bool newLine = true;

        foreach (char c in lines)
        {
            if (newLine)
            {
                BeginLine();
                newLine = false;
            }
            if (c == '\n')
            {
                EndLine();
                newLine = true;
                continue;
            }

            builder.Append(c);
        }

        if (!newLine)
            EndLine();
    }

    public void BeginLine()
    {
        for (int i = 0; i < indent; i++)
        {
            builder.Append(indent_string);
        }
    }

    public void EndLine() => builder.Append(new_line_char);

    public void BlankLine() => EndLine();

    public override string ToString() => builder.ToString();

    public readonly struct Block(SourceBuilder reference) : IDisposable
    {
        private readonly SourceBuilder builderReference = reference;

        public void Dispose()
        {
            builderReference.indent -= 1;
            builderReference.AddLine("}");
        }
    }
}

