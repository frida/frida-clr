namespace Frida;

public class FridaCrash
{
    private readonly Crash _crash;

    internal FridaCrash(Crash crash)
    {
        _crash = crash;
    }
    
    public uint Pid => _crash.Pid;
    public string? ProcessName => _crash.ProcessName;
    public string? Summary => _crash.Summary;
    public string? Report => _crash.Report;
}