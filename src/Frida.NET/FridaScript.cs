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

    public void Load()
    {
        _script.LoadSync(null);
    }
    
    public void Unload()
    {
        _script.UnloadSync(null);
    }

    public void EnableDebugger(ushort port)
    {
        _script.EnableDebuggerSync(port, null);
    }
    
    public void DisableDebugger()
    {
        _script.DisableDebuggerSync(null);
    }

    public void Post(string json, byte[]? bytes = null)
    {
        _script.Post(json, bytes != null ? Bytes.New(bytes) : null);
    }

    public void Eternalize()
    {
        _script.EternalizeSync(null);
    }

    public bool IsDestroyed()
    {
        return _script.IsDestroyed();
    }
    
    private void HandleMessage(Script script, Script.MessageSignalArgs eventArgs)
    {
        // TODO: Consume eventArgs.Bytes
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