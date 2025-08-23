namespace Frida.Data;

public class ScriptOptions
{
    public string? Name { get; set; }
    public ScriptRuntime? Runtime { get; set; }
    public byte[]? Snapshot { get; set; }
    public SnapshotTransport? SnapshotTransport { get; set; }
}