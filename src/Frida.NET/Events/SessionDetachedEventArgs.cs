namespace Frida.Events;

public class SessionDetachedEventArgs : EventArgs
{
    public SessionDetachedEventArgs(SessionDetachReason reason, FridaCrash? crash)
    {
        Reason = reason;
        Crash = crash;
    }

    public SessionDetachReason Reason { get; }
    public FridaCrash? Crash { get; }
}