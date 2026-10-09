using System.Runtime.InteropServices;

namespace Frida;

internal static class FridaRuntime
{
    public static Task Invoke(Action<Gio.Internal.AsyncReadyCallback> begin, Action<Gio.AsyncResult> finish)
    {
        return Invoke<object?>(begin, res =>
        {
            finish(res);
            return null;
        });
    }

    public static Task<T> Invoke<T>(Action<Gio.Internal.AsyncReadyCallback> begin, Func<Gio.AsyncResult, T> finish)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        GCHandle readyRoot = default;
        var ready = new Gio.Internal.AsyncReadyCallbackAsyncHandler((_, res, _) =>
        {
            try
            {
                completion.SetResult(finish(res));
            }
            catch (Exception e)
            {
                completion.SetException(e);
            }

            readyRoot.Free();
        });
        readyRoot = GCHandle.Alloc(ready);

        ScheduleOnFridaThread(() =>
        {
            try
            {
                begin(ready.NativeCallback);
            }
            catch (Exception e)
            {
                completion.SetException(e);
                readyRoot.Free();
            }

            return false;
        });

        return completion.Task;
    }

    private static void ScheduleOnFridaThread(Func<bool> action)
    {
        GCHandle root = default;
        SourceFunc source = _ =>
        {
            try
            {
                return action();
            }
            finally
            {
                root.Free();
            }
        };
        root = GCHandle.Alloc(source);

        g_main_context_invoke_full(frida_get_main_context(), 0, source, IntPtr.Zero, IntPtr.Zero);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool SourceFunc(IntPtr data);

    [DllImport("Frida", EntryPoint = "frida_get_main_context", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr frida_get_main_context();

    [DllImport("Frida", EntryPoint = "g_main_context_invoke_full", CallingConvention = CallingConvention.Cdecl)]
    private static extern void g_main_context_invoke_full(IntPtr context, int priority, SourceFunc function, IntPtr data, IntPtr notify);
}
