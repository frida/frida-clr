namespace Frida.Events;

public class ScriptMessageEventArgs : EventArgs
{
    public ScriptMessageEventArgs(string json, byte[]? data)
    {
        Json = json;
        Data = data;
    }

    public string Json { get; }
    public byte[]? Data { get; }
}
