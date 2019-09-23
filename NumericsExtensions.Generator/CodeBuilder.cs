using System;
using System.Text;

namespace NumericsExtensions.Generator
{
    class CodeBuilder
    {
        int indent = 0;
        StringBuilder stringBuilder = new StringBuilder();
        struct BlockEnvironment : IDisposable
        {
            CodeBuilder parent;

            public BlockEnvironment(CodeBuilder parent)
            {
                this.parent = parent;
                parent.WriteLine("{");
                parent.indent++;

            }
            void IDisposable.Dispose()
            {
                parent.indent--;
                parent.WriteLine("}");

            }
        }
        bool indentWritten = false;
        const int indentSize = 4;
        public void Write(string text)
        {
            ReadOnlySpan<char> remaining = text.AsSpan();
            for (int i = 0; i < remaining.Length; i++)
            {
                char c = remaining[i];
                if (c == '\n' || c == '\r')
                    indentWritten = false;
                if (!indentWritten && !char.IsWhiteSpace(c))
                {
                    int count = indent * indentSize;
                    for (int j = 0; j < count; j++)
                        stringBuilder.Append(' ');
                    indentWritten = true;
                }
                stringBuilder.Append(c);
            }
        }
        public void WriteLine(string text)
        {
            Write(text);
            stringBuilder.Append('\r');
            stringBuilder.Append('\n');
            indentWritten = false;
        }
        public IDisposable Block() => new BlockEnvironment(this);
        public IDisposable Block(string header)
        {
            WriteLine(header);
            return new BlockEnvironment(this);
        }
        public override string ToString() => stringBuilder.ToString();
    }
}
