using System.Text.Json;
using Frida.Events;
using Frida.Helpers;
using GLib;

namespace Frida;

public partial class FridaScript
{
    private readonly Dictionary<long, TaskCompletionSource<JsonElement>> _pendingCalls = new();
    private long _nextCallId = 1;
    private LazyEvent<ScriptMessageEventArgs> _onMessage = null!;

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

    partial void OnConstructed()
    {
        _onMessage = new LazyEvent<ScriptMessageEventArgs>(
            _ => _script.OnMessage += HandleMessage,
            _ => _script.OnMessage -= HandleMessage);
    }

    partial void OnDispose()
    {
        _onMessage.Dispose();
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

    public event EventHandler<ScriptMessageEventArgs> OnMessage
    {
        add => _onMessage.Add(value);
        remove => _onMessage.Remove(value);
    }
}
