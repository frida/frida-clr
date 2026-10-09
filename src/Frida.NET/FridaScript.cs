using System.Text.Json;
using Frida.Events;
using Frida.Helpers;
using GLib;

namespace Frida;

public class FridaScript : IDisposable
{
    private readonly Script _script;
    private readonly LazyEvent<ScriptMessageEventArgs> _onMessage;
    private readonly LazyEvent<ScriptDestroyedEventArgs> _onDestroyed;
    private readonly Dictionary<long, TaskCompletionSource<JsonElement>> _pendingCalls = new();
    private long _nextCallId = 1;
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

    public Task<JsonElement> Call(string method, params object?[] args)
    {
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        long id;
        lock (_pendingCalls)
        {
            id = _nextCallId++;
            _pendingCalls[id] = completion;
        }

        object?[] request = { "frida:rpc", id, "call", method, args };
        Post(JsonSerializer.Serialize(request));

        return completion.Task;
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
        if (HasPendingCalls() && TryCompleteCall(eventArgs.Json))
            return;

        _onMessage.InvokeHandlers(this, new ScriptMessageEventArgs(eventArgs.Json, ExtractData(eventArgs.Data)));
    }

    private bool HasPendingCalls()
    {
        lock (_pendingCalls)
            return _pendingCalls.Count > 0;
    }

    private bool TryCompleteCall(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.GetProperty("type").GetString() != "send")
            return false;
        if (!root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Array || payload.GetArrayLength() < 3)
            return false;
        if (payload[0].GetString() != "frida:rpc")
            return false;

        var id = payload[1].GetInt64();
        TaskCompletionSource<JsonElement>? completion;
        lock (_pendingCalls)
        {
            if (!_pendingCalls.Remove(id, out completion))
                return true;
        }

        if (payload[2].GetString() == "ok")
            completion.SetResult(payload.GetArrayLength() > 3 ? payload[3].Clone() : default);
        else
            completion.SetException(new Exception(payload.GetArrayLength() > 3 ? payload[3].GetString() : "RPC call failed"));

        return true;
    }

    private static byte[]? ExtractData(GLib.Bytes? data)
    {
        if (data == null)
            return null;

        var size = data.GetSize();
        return (size == 0) ? System.Array.Empty<byte>() : data.GetRegionSpan<byte>(0, size).ToArray();
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
