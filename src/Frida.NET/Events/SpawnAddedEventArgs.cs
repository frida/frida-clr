namespace Frida.Events;

public class SpawnAddedEventArgs : EventArgs
{
    public SpawnAddedEventArgs(uint pid, string? identifier)
    {
        Pid = pid;
        Identifier = identifier;
    }

    public uint Pid { get; }
    public string? Identifier { get; }
}
