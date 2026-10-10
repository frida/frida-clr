using Frida.Events;
using Frida.Helpers;

namespace Frida;

public class FridaDevice : IDisposable
{
    private readonly Device _device;
    private readonly LazyEvent<DeviceLostEventArgs> _onLost;
    private readonly LazyEvent<SpawnAddedEventArgs> _onSpawnAdded;
    private bool _disposed;

    internal FridaDevice(Device device)
    {
        _device = device;
        _onLost = new LazyEvent<DeviceLostEventArgs>(
            _ => _device.OnLost += HandleLost,
            _ => _device.OnLost -= HandleLost);
        _onSpawnAdded = new LazyEvent<SpawnAddedEventArgs>(
            _ => _device.OnSpawnAdded += HandleSpawnAdded,
            _ => _device.OnSpawnAdded -= HandleSpawnAdded);
    }

    public string? Id => _device.Id;
    public string? Name => _device.Name;
    public DeviceType Type => _device.Dtype;

    public Task EnableSpawnGating()
    {
        return FridaRuntime.Invoke(
            ready => Internal.Device.EnableSpawnGating(_device.Handle.DangerousGetHandle(), IntPtr.Zero, IntPtr.Zero, ready, IntPtr.Zero),
            _device.EnableSpawnGatingFinish);
    }

    public Task DisableSpawnGating()
    {
        return FridaRuntime.Invoke(
            ready => Internal.Device.DisableSpawnGating(_device.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _device.DisableSpawnGatingFinish);
    }

    public async Task<IReadOnlyList<FridaApplication>> EnumerateApplications(Scope scope = Scope.Minimal)
    {
        var options = ApplicationQueryOptions.New();
        options.Scope = scope;

        using var list = await FridaRuntime.Invoke(
            ready => Internal.Device.EnumerateApplications(_device.Handle.DangerousGetHandle(), options.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _device.EnumerateApplicationsFinish).ConfigureAwait(false);

        var applications = new List<FridaApplication>();
        for (var i = 0; i < list.Size(); i++)
            applications.Add(new FridaApplication(list.Get(i)));
        return applications;
    }

    public async Task<IReadOnlyList<FridaProcess>> EnumerateProcesses(Scope scope = Scope.Minimal)
    {
        var options = ProcessQueryOptions.New();
        options.Scope = scope;

        using var list = await FridaRuntime.Invoke(
            ready => Internal.Device.EnumerateProcesses(_device.Handle.DangerousGetHandle(), options.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _device.EnumerateProcessesFinish).ConfigureAwait(false);

        var processes = new List<FridaProcess>();
        for (var i = 0; i < list.Size(); i++)
            processes.Add(new FridaProcess(list.Get(i)));
        return processes;
    }

    public Task<uint> Spawn(string program, Data.SpawnOptions? spawnOptions = null)
    {
        var options = BuildSpawnOptions(spawnOptions);
        return FridaRuntime.Invoke(
            ready =>
            {
                using var programNative = GLib.Internal.NonNullableUtf8StringOwnedHandle.Create(program);
                Internal.Device.Spawn(_device.Handle.DangerousGetHandle(), programNative, options.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero);
            },
            _device.SpawnFinish);
    }

    public Task Resume(uint pid)
    {
        return FridaRuntime.Invoke(
            ready => Internal.Device.Resume(_device.Handle.DangerousGetHandle(), pid, IntPtr.Zero, ready, IntPtr.Zero),
            _device.ResumeFinish);
    }

    public async Task<FridaSession> Attach(uint pid, Data.SessionOptions? sessionOptions = null)
    {
        var options = BuildSessionOptions(sessionOptions);
        var session = await FridaRuntime.Invoke(
            ready => Internal.Device.Attach(_device.Handle.DangerousGetHandle(), pid, options.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _device.AttachFinish).ConfigureAwait(false);
        return new FridaSession(session);
    }

    private static SpawnOptions BuildSpawnOptions(Data.SpawnOptions? spawnOptions)
    {
        var options = SpawnOptions.New();
        if (spawnOptions == null)
            return options;

        if (spawnOptions.Argv is { Length: > 0 })
            options.Argv = spawnOptions.Argv;
        if (spawnOptions.Envp is { Length: > 0 })
            options.Envp = spawnOptions.Envp;
        if (spawnOptions.Env is { Length: > 0 })
            options.Env = spawnOptions.Env;
        if (spawnOptions.Cwd != null)
            options.Cwd = spawnOptions.Cwd;
        if (spawnOptions.Stdio != null)
            options.Stdio = spawnOptions.Stdio.Value;
        return options;
    }

    private static SessionOptions BuildSessionOptions(Data.SessionOptions? sessionOptions)
    {
        var options = SessionOptions.New();
        if (sessionOptions == null)
            return options;

        if (sessionOptions.Realm != null)
            options.Realm = sessionOptions.Realm.Value;
        if (sessionOptions.PersistTimeout.HasValue)
            options.PersistTimeout = sessionOptions.PersistTimeout.Value;
        if (sessionOptions.EmulatedAgentPath != null)
            options.EmulatedAgentPath = sessionOptions.EmulatedAgentPath;
        return options;
    }

    private void HandleLost(Device sender, EventArgs eventArgs)
    {
        _onLost.InvokeHandlers(this, new DeviceLostEventArgs());
    }

    private void HandleSpawnAdded(Device sender, Device.SpawnAddedSignalArgs eventArgs)
    {
        var spawn = eventArgs.Spawn;
        _onSpawnAdded.InvokeHandlers(this, new SpawnAddedEventArgs(spawn.GetPid(), spawn.GetIdentifier()));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _onLost.Dispose();
        _onSpawnAdded.Dispose();
        _device.Dispose();
    }

    public event EventHandler<DeviceLostEventArgs> OnLost
    {
        add => _onLost.Add(value);
        remove => _onLost.Remove(value);
    }

    public event EventHandler<SpawnAddedEventArgs> OnSpawnAdded
    {
        add => _onSpawnAdded.Add(value);
        remove => _onSpawnAdded.Remove(value);
    }
}
