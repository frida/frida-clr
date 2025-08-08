using Frida.Events;
using Frida.Helpers;

namespace Frida;

public class FridaDevice : IDisposable
{
    private readonly Device _device;
    private readonly LazyEvent<DeviceLostEventArgs> _onLost;
    private bool _disposed;

    internal FridaDevice(Device device)
    {
        _device = device;
        _onLost = new LazyEvent<DeviceLostEventArgs>(
            _ => _device.OnLost += HandleLost,
            _ => _device.OnLost -= HandleLost);
    }
    
    public string? Id => _device.Id;
    public string? Name => _device.Name;
    public DeviceType Type => _device.Dtype;

    public void EnableSpawnGating()
    {
        _device.EnableSpawnGatingSync(null);
    }

    public void DisableSpawnGating()
    {
        _device.DisableSpawnGatingSync(null);
    }

    public IEnumerable<FridaApplication> EnumerateApplications(Scope scope = Scope.Minimal)
    {
        var applicationQueryOptions = ApplicationQueryOptions.New();
        
        applicationQueryOptions.Scope = scope;
        
        var applicationList = _device.EnumerateApplicationsSync(applicationQueryOptions, null);
        
        for (var i = 0; i < applicationList.Size(); i++)
        {
            yield return new FridaApplication(applicationList.Get(i));
        }
    }
    
    public IEnumerable<FridaProcess> EnumerateProcesses(Scope scope = Scope.Minimal)
    {
        var processQueryOptions = ProcessQueryOptions.New();
        
        processQueryOptions.Scope = scope;
        
        var processList = _device.EnumerateProcessesSync(processQueryOptions, null);

        for (var i = 0; i < processList.Size(); i++)
        {
            yield return new FridaProcess(processList.Get(i));
        }
    }

    public uint Spawn(string program, Data.SpawnOptions? spawnOptions = null)
    {
        var internalSpawnOptions = SpawnOptions.New();

        if (spawnOptions != null)
        {
            if (spawnOptions.Argv != null && 
                spawnOptions.Argv.Length > 0)
            {
                internalSpawnOptions.Argv = spawnOptions.Argv;
            }
            
            if (spawnOptions.Envp != null && 
                spawnOptions.Envp.Length > 0)
            {
                internalSpawnOptions.Envp = spawnOptions.Envp;
            }
            
            if (spawnOptions.Env != null && 
                spawnOptions.Env.Length > 0)
            {
                internalSpawnOptions.Env = spawnOptions.Env;
            }
            
            if (spawnOptions.Cwd != null)
            {
                internalSpawnOptions.Cwd = spawnOptions.Cwd;
            }
            
            if (spawnOptions.Stdio != null)
            {
                internalSpawnOptions.Stdio = spawnOptions.Stdio.Value;
            }
        }
        
        return _device.SpawnSync(program, internalSpawnOptions, null);
    }

    public void Resume(uint pid)
    {
        _device.ResumeSync(pid, null);
    }

    public FridaSession Attach(uint pid, Data.SessionOptions? sessionOptions = null)
    {
        var internalSessionOptions = SessionOptions.New();

        if (sessionOptions != null)
        {
            if (sessionOptions.Realm != null)
            {
                internalSessionOptions.Realm = sessionOptions.Realm.Value;
            }
            
            if (sessionOptions.PersistTimeout.HasValue)
            {
                internalSessionOptions.PersistTimeout = sessionOptions.PersistTimeout.Value;
            }
            
            if (sessionOptions.EmulatedAgentPath != null)
            {
                internalSessionOptions.EmulatedAgentPath = sessionOptions.EmulatedAgentPath;
            }
        }
        
        var session = _device.AttachSync(pid, internalSessionOptions, null);
        
        return new FridaSession(session);
    }
    
    private void HandleLost(Device sender, EventArgs eventArgs)
    {
        _onLost.InvokeHandlers(this, new DeviceLostEventArgs());
    }
    
    public void Dispose()
    {
        if (_disposed) 
        {
            return;   
        }
        
        _disposed = true;
        _onLost.Dispose();
        _device.Dispose();
    }

    public event EventHandler<DeviceLostEventArgs> OnLost
    {
        add => _onLost.Add(value);
        remove => _onLost.Remove(value);
    }
}