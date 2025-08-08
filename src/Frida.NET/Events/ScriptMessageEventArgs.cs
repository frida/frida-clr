namespace Frida.Events;

public class ScriptMessageEventArgs : EventArgs
{
    public ScriptMessageEventArgs(string json)
    {
        Json = json;
    }
    
    public string Json { get; }
}