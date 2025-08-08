namespace Frida.Events;

public class DeviceAddedEventArgs : EventArgs
{
    public DeviceAddedEventArgs(FridaDevice device)
    {
        Device = device;
    }
    
    public FridaDevice Device { get; }
}