namespace Frida.Data;

public class SessionOptions
{
    public Realm? Realm { get; set; }
    
    public uint? PersistTimeout { get; set; }
    
    public string? EmulatedAgentPath { get; set; }
}