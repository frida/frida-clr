namespace Frida;

public class FridaProcess
{
    private readonly Process _process;

    internal FridaProcess(Process process)
    {
        _process = process;
    }

    public uint Pid => _process.Pid;
    public string? Name => _process.Name;
}