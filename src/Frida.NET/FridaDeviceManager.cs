namespace Frida;

public partial class FridaDeviceManager
{
    internal DeviceManager Manager => _deviceManager;

    public FridaDeviceManager()
        : this(DeviceManager.New())
    {
    }

    public async Task<FridaDevice?> FindDevice(DevicePredicate predicate, TimeSpan timeout)
    {
        var predicateHandler = new Internal.DeviceManager.PredicateCallHandler(x => predicate(new FridaDevice(x)));
        var device = await FridaRuntime.Invoke(
            ready => Internal.DeviceManager.FindDevice(_deviceManager.Handle.DangerousGetHandle(), predicateHandler.NativeCallback, IntPtr.Zero, (int)timeout.TotalMilliseconds, IntPtr.Zero, ready, IntPtr.Zero),
            _deviceManager.FindDeviceFinish).ConfigureAwait(false);
        return device != null ? new FridaDevice(device) : null;
    }

    public delegate bool DevicePredicate(FridaDevice device);
}
