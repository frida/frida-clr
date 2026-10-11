using System.Text;

internal sealed class SourceWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public void Line(string text)
    {
        if (text.Length == 0)
        {
            _builder.Append('\n');
            return;
        }

        _builder.Append(new string(' ', _indent * 4));
        _builder.Append(text);
        _builder.Append('\n');
    }

    public void Blank() => _builder.Append('\n');

    public void Open()
    {
        Line("{");
        _indent++;
    }

    public void Close()
    {
        _indent--;
        Line("}");
    }

    public override string ToString() => _builder.ToString();
}
