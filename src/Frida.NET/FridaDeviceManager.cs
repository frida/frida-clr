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

    public FridaDevice? FindDevice(DevicePredicate predicate, TimeSpan timeout)
    {
        var device = _deviceManager.FindDeviceSync(x => predicate(new FridaDevice(x)), (int)timeout.TotalMilliseconds, null);
        if (device == null) return null;
        
        return new FridaDevice(device);
    }
    
    public FridaDevice? FindDeviceById(string id, TimeSpan timeout)
    {
        var device = _deviceManager.FindDeviceByIdSync(id, (int)timeout.TotalMilliseconds, null);
        if (device == null) return null;
        
        return new FridaDevice(device);
    }

    public FridaDevice? FindDeviceByType(DeviceType deviceType, TimeSpan timeout)
    {
        var device = _deviceManager.FindDeviceByTypeSync(deviceType, (int)timeout.TotalMilliseconds, null);
        if (device == null) return null;
        
        return new FridaDevice(device);
    }
    
    public IEnumerable<FridaDevice> EnumerateDevices()
    {
        using var deviceList = _deviceManager.EnumerateDevicesSync(null);

        for (var i = 0; i < deviceList.Size(); i++)
        {
            yield return new FridaDevice(deviceList.Get(i));
        }
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