namespace Frida;

public class FridaCompiler : IDisposable
{
    private readonly Compiler _compiler;
    private bool _disposed;

    public event Action<string>? Diagnostics;

    public FridaCompiler(FridaDeviceManager deviceManager)
    {
        _compiler = Compiler.New(deviceManager.Manager);
        _compiler.OnDiagnostics += HandleDiagnostics;
    }

    public Task<string> Build(string entrypoint, FridaCompilerOptions? options = null)
    {
        return FridaRuntime.Invoke(
            ready =>
            {
                var nativeOptions = (options ?? new FridaCompilerOptions()).ToNative();
                using var entrypointNative = GLib.Internal.NonNullableUtf8StringOwnedHandle.Create(entrypoint);
                Internal.Compiler.Build(_compiler.Handle.DangerousGetHandle(), entrypointNative,
                    nativeOptions.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero);
            },
            _compiler.BuildFinish);
    }

    private void HandleDiagnostics(Compiler sender, Compiler.DiagnosticsSignalArgs args)
    {
        Diagnostics?.Invoke(args.Diagnostics.Print(false));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _compiler.OnDiagnostics -= HandleDiagnostics;
    }
}
