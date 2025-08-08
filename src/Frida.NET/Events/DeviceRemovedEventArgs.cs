namespace Frida.Events;

public class DeviceRemovedEventArgs : EventArgs
{
    public DeviceRemovedEventArgs(FridaDevice device)
    {
        Device = device;
    }
    
    public FridaDevice Device { get; }
}