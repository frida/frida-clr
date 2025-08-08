namespace Frida.Data;

public class SpawnOptions
{
    public string[]? Argv { get; set; }
    
    public string[]? Envp { get; set; }
    
    public string[]? Env { get; set; }
    
    public string? Cwd { get; set; }
    
    public Stdio? Stdio { get; set; }
}