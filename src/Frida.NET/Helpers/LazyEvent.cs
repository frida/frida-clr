namespace Frida.Helpers;

internal class LazyEvent<TEventArgs> : IDisposable where TEventArgs : EventArgs
{
    private readonly Action<EventHandler<TEventArgs>> _subscribeInternal;
    private readonly Action<EventHandler<TEventArgs>> _unsubscribeInternal;

    private EventHandler<TEventArgs>? _handlers;

    public LazyEvent(
        Action<EventHandler<TEventArgs>> subscribeInternal,
        Action<EventHandler<TEventArgs>> unsubscribeInternal)
    {
        _subscribeInternal = subscribeInternal;
        _unsubscribeInternal = unsubscribeInternal;
    }

    public void Add(EventHandler<TEventArgs> handler)
    {
        if (IsEmpty())
        {
            _subscribeInternal(InvokeHandlers);
        }

        _handlers += handler;
    }

    public void Remove(EventHandler<TEventArgs> handler)
    {
        _handlers -= handler;

        if (IsEmpty())
        {
            _unsubscribeInternal(InvokeHandlers);
        }
    }

    public void InvokeHandlers(object? sender, TEventArgs args)
    {
        _handlers?.Invoke(sender, args);
    }

    private bool IsEmpty()
    {
        return _handlers == null || _handlers.GetInvocationList().Length == 0;
    }

    public void Dispose()
    {
        _unsubscribeInternal(InvokeHandlers);
    }
}