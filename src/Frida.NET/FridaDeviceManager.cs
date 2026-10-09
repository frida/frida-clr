using Frida.Events;
using Frida.Helpers;

namespace Frida;

public class FridaDeviceManager : IDisposable
{
    private readonly DeviceManager _deviceManager;
    private readonly LazyEvent<DeviceChangedEventArgs> _onDeviceChanged;
    private readonly LazyEvent<DeviceAddedEventArgs> _onDeviceAdded;
    private readonly LazyEvent<DeviceRemovedEventArgs> _onDeviceRemoved;
    private bool _disposed;

    internal DeviceManager Manager => _deviceManager;

    public FridaDeviceManager()
    {
        _deviceManager = DeviceManager.New();

        _onDeviceAdded = new LazyEvent<DeviceAddedEventArgs>(
            _ => _deviceManager.OnAdded += HandleAdded,
            _ => _deviceManager.OnAdded -= HandleAdded);

        _onDeviceRemoved = new LazyEvent<DeviceRemovedEventArgs>(
            _ => _deviceManager.OnRemoved += HandleRemoved,
            _ => _deviceManager.OnRemoved -= HandleRemoved);

        _onDeviceChanged = new LazyEvent<DeviceChangedEventArgs>(
            _ => _deviceManager.OnChanged += HandleChanged,
            _ => _deviceManager.OnChanged -= HandleChanged);
    }

    public async Task<FridaDevice?> FindDevice(DevicePredicate predicate, TimeSpan timeout)
    {
        var predicateHandler = new Internal.DeviceManager.PredicateCallHandler(x => predicate(new FridaDevice(x)));
        var device = await FridaRuntime.Invoke(
            ready => Internal.DeviceManager.FindDevice(_deviceManager.Handle.DangerousGetHandle(), predicateHandler.NativeCallback, IntPtr.Zero, (int)timeout.TotalMilliseconds, IntPtr.Zero, ready, IntPtr.Zero),
            _deviceManager.FindDeviceFinish).ConfigureAwait(false);
        return device != null ? new FridaDevice(device) : null;
    }

    public async Task<FridaDevice?> FindDeviceById(string id, TimeSpan timeout)
    {
        var device = await FridaRuntime.Invoke(
            ready =>
            {
                using var idNative = GLib.Internal.NonNullableUtf8StringOwnedHandle.Create(id);
                Internal.DeviceManager.FindDeviceById(_deviceManager.Handle.DangerousGetHandle(), idNative, (int)timeout.TotalMilliseconds, IntPtr.Zero, ready, IntPtr.Zero);
            },
            _deviceManager.FindDeviceByIdFinish).ConfigureAwait(false);
        return device != null ? new FridaDevice(device) : null;
    }

    public async Task<FridaDevice?> FindDeviceByType(DeviceType deviceType, TimeSpan timeout)
    {
        var device = await FridaRuntime.Invoke(
            ready => Internal.DeviceManager.FindDeviceByType(_deviceManager.Handle.DangerousGetHandle(), deviceType, (int)timeout.TotalMilliseconds, IntPtr.Zero, ready, IntPtr.Zero),
            _deviceManager.FindDeviceByTypeFinish).ConfigureAwait(false);
        return device != null ? new FridaDevice(device) : null;
    }

    public async Task<IReadOnlyList<FridaDevice>> EnumerateDevices()
    {
        using var deviceList = await FridaRuntime.Invoke(
            ready => Internal.DeviceManager.EnumerateDevices(_deviceManager.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _deviceManager.EnumerateDevicesFinish).ConfigureAwait(false);

        var devices = new List<FridaDevice>();
        for (var i = 0; i < deviceList.Size(); i++)
            devices.Add(new FridaDevice(deviceList.Get(i)));
        return devices;
    }

    private void HandleAdded(DeviceManager sender, DeviceManager.AddedSignalArgs args)
    {
        _onDeviceAdded.InvokeHandlers(this, new DeviceAddedEventArgs(new FridaDevice(args.Device)));
    }

    private void HandleRemoved(DeviceManager sender, DeviceManager.RemovedSignalArgs args)
    {
        _onDeviceRemoved.InvokeHandlers(this, new DeviceRemovedEventArgs(new FridaDevice(args.Device)));
    }
    
    private void HandleChanged(DeviceManager sender, EventArgs args)
    {
        _onDeviceChanged.InvokeHandlers(this, new DeviceChangedEventArgs());
    }

    public void Dispose()
    {
        if (_disposed) 
        {
            return;   
        }
        
        _disposed = true;
        _onDeviceAdded.Dispose();
        _onDeviceRemoved.Dispose();
        _onDeviceChanged.Dispose();
        _deviceManager.Dispose();
    }

    public event EventHandler<DeviceAddedEventArgs> OnDeviceAdded
    {
        add => _onDeviceAdded.Add(value);
        remove => _onDeviceAdded.Remove(value);
    }

    public event EventHandler<DeviceRemovedEventArgs> OnDeviceRemoved
    {
        add => _onDeviceRemoved.Add(value);
        remove => _onDeviceRemoved.Remove(value);
    }

    public event EventHandler<DeviceChangedEventArgs> OnDeviceChanged
    {
        add => _onDeviceChanged.Add(value);
        remove => _onDeviceChanged.Remove(value);
    }
    
    public delegate bool DevicePredicate(FridaDevice device);
}