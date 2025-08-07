namespace Frida;

public class FridaDeviceManager : IDisposable
{
    private readonly DeviceManager _deviceManager;
    
    public FridaDeviceManager()
    {
        _deviceManager = DeviceManager.New();
    }

    public IEnumerable<FridaDevice> EnumerateDevices()
    {
        using var deviceList = _deviceManager.EnumerateDevicesSync(null);

        for (var i = 0; i < deviceList.Size(); i++)
        {
            yield return new FridaDevice(deviceList.Get(i));
        }
    }

    public void Dispose()
    {
        _deviceManager.Dispose();
    }
}