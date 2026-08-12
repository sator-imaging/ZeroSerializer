// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System.Text;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Generator;

internal sealed class GeneratedSourceBuilder
{
    private readonly StringBuilder sourceBuilder = new();
    private int indentationLevel;

    internal void AppendLine(string line = "")
    {
        if (line.Length != 0)
        {
            sourceBuilder.Append(' ', indentationLevel * 4);
            sourceBuilder.Append(line);
        }

        sourceBuilder.AppendLine();
    }

    internal void AppendDirective(string line)
    {
        sourceBuilder.AppendLine(line);
    }

    internal void OpenBlock()
    {
        AppendLine("{");
        indentationLevel++;
    }

    internal void CloseBlock()
    {
        indentationLevel--;
        AppendLine("}");
    }

    public override string ToString()
    {
        return sourceBuilder.ToString();
    }
}
