namespace Frida;

public class FridaDevice
{
    private readonly Device _device;

    internal FridaDevice(Device device)
    {
        _device = device;
    }
    
    public string? Id => _device.Id;
    public string? Name => _device.Name;
    public DeviceType Type => _device.Dtype;
}