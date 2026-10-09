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

    public async Task<FridaScript> CreateScript(string source, Data.ScriptOptions? options = null)
    {
        var scriptOptions = BuildScriptOptions(options);
        var script = await FridaRuntime.Invoke(
            ready =>
            {
                using var sourceNative = GLib.Internal.NonNullableUtf8StringOwnedHandle.Create(source);
                Internal.Session.CreateScript(_session.Handle.DangerousGetHandle(), sourceNative, scriptOptions.Handle.DangerousGetHandle(), IntPtr.Zero, ready, IntPtr.Zero);
            },
            _session.CreateScriptFinish).ConfigureAwait(false);
        return new FridaScript(script);
    }

    private static ScriptOptions BuildScriptOptions(Data.ScriptOptions? options)
    {
        var scriptOptions = ScriptOptions.New();
        if (options == null)
            return scriptOptions;

        if (options.Name != null)
            scriptOptions.Name = options.Name;
        if (options.Runtime != null)
            scriptOptions.Runtime = options.Runtime.Value;
        if (options.Snapshot != null)
            scriptOptions.Snapshot = Bytes.New(options.Snapshot);
        if (options.SnapshotTransport != null)
            scriptOptions.SnapshotTransport = options.SnapshotTransport.Value;
        return scriptOptions;
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
