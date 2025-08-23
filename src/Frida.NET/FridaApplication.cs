namespace Frida;

public class FridaApplication
{
    private readonly Application _application;

    internal FridaApplication(Application application)
    {
        _application = application;
    }

    public uint Pid => _application.Pid;
    public string? Name => _application.Name;
    public string? Identifier => _application.Identifier;
}