using Frida.Events;
using Frida.Helpers;
using GLib;

namespace Frida;

public class FridaSession : IDisposable
{
    private readonly Session _session;
    private readonly LazyEvent<SessionDetachedEventArgs> _onDetached;
    private bool _disposed;

    internal FridaSession(Session session)
    {
        _session = session;
        _onDetached = new LazyEvent<SessionDetachedEventArgs>(
            _ => _session.OnDetached += HandleDetached,
            _ => _session.OnDetached -= HandleDetached);
    }

    public FridaScript CreateScript(string source, Data.ScriptOptions? options = null)
    {
        var internalScriptOptions = ScriptOptions.New();

        if (options != null)
        {
            if (options.Name != null)
            {
                internalScriptOptions.Name = options.Name;
            }

            if (options.Runtime != null)
            {
                internalScriptOptions.Runtime = options.Runtime.Value;
            }

            if (options.Snapshot != null)
            {
                internalScriptOptions.Snapshot = Bytes.New(options.Snapshot);
            }
            
            if (options.SnapshotTransport != null)
            {
                internalScriptOptions.SnapshotTransport = options.SnapshotTransport.Value;
            }
        }
        
        var script = _session.CreateScriptSync(source, internalScriptOptions, null);

        return new FridaScript(script);
    }
    
    private void HandleDetached(Session session, Session.DetachedSignalArgs args)
    {
        _onDetached.InvokeHandlers(this, new SessionDetachedEventArgs(
            args.Reason, 
            args.Crash != null ? new FridaCrash(args.Crash) : null));
    }

    public void Dispose()
    {
        if (_disposed) 
        {
            return;   
        }
        
        _disposed = true;
        _onDetached.Dispose();
        _session.Dispose();
    }

    public event EventHandler<SessionDetachedEventArgs> OnDetached
    {
        add => _onDetached.Add(value);
        remove => _onDetached.Remove(value);
    }
}