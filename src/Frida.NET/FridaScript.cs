using Frida.Events;
using Frida.Helpers;
using GLib;

namespace Frida;

public class FridaScript : IDisposable
{
    private readonly Script _script;
    private readonly LazyEvent<ScriptMessageEventArgs> _onMessage;
    private readonly LazyEvent<ScriptDestroyedEventArgs> _onDestroyed;
    private bool _disposed;

    internal FridaScript(Script script)
    {
        _script = script;
        _onMessage = new LazyEvent<ScriptMessageEventArgs>(
            _ => _script.OnMessage += HandleMessage,
            _ => _script.OnMessage -= HandleMessage);
        _onDestroyed = new LazyEvent<ScriptDestroyedEventArgs>(
            _ => _script.OnDestroyed += HandleDestroyed,
            _ => _script.OnDestroyed -= HandleDestroyed);
    }

    public Task Load()
    {
        return FridaRuntime.Invoke(
            ready => Internal.Script.Load(_script.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _script.LoadFinish);
    }

    public Task Unload()
    {
        return FridaRuntime.Invoke(
            ready => Internal.Script.Unload(_script.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _script.UnloadFinish);
    }

    public Task EnableDebugger(ushort port)
    {
        return FridaRuntime.Invoke(
            ready => Internal.Script.EnableDebugger(_script.Handle.DangerousGetHandle(), port, IntPtr.Zero, ready, IntPtr.Zero),
            _script.EnableDebuggerFinish);
    }

    public Task DisableDebugger()
    {
        return FridaRuntime.Invoke(
            ready => Internal.Script.DisableDebugger(_script.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _script.DisableDebuggerFinish);
    }

    public Task Eternalize()
    {
        return FridaRuntime.Invoke(
            ready => Internal.Script.Eternalize(_script.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero),
            _script.EternalizeFinish);
    }

    public void Post(string json, byte[]? bytes = null)
    {
        _script.Post(json, bytes != null ? Bytes.New(bytes) : null);
    }

    public bool IsDestroyed()
    {
        return _script.IsDestroyed();
    }

    private void HandleMessage(Script script, Script.MessageSignalArgs eventArgs)
    {
        _onMessage.InvokeHandlers(this, new ScriptMessageEventArgs(eventArgs.Json));
    }

    private void HandleDestroyed(Script script, EventArgs eventArgs)
    {
        _onDestroyed.InvokeHandlers(this, new ScriptDestroyedEventArgs());
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        _onMessage.Dispose();
        _onDestroyed.Dispose();
        _script.Dispose();
    }

    public event EventHandler<ScriptMessageEventArgs> OnMessage
    {
        add => _onMessage.Add(value);
        remove => _onMessage.Remove(value);
    }

    public event EventHandler<ScriptDestroyedEventArgs> OnDestroyed
    {
        add => _onDestroyed.Add(value);
        remove => _onDestroyed.Remove(value);
    }
}