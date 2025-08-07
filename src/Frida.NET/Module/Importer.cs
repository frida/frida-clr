using System.Reflection;
using System.Runtime.InteropServices;

namespace Frida.Module;

internal static class Importer
{
    private static readonly HashSet<string> Libraries =
    [
        "GLib",
        "GObject",
        "Gio",
        "Frida",
    ];
    
    private const string WindowsLibraryName = "Frida.dll";
    private const string LinuxLibraryName = "Frida.so";
    private const string OsxLibraryName = "Frida.dylib";

    private static IntPtr TargetLibraryPointer = IntPtr.Zero;
    
    public static void RegisterAsDllImportResolver(Assembly assembly)
    {
        NativeLibrary.SetDllImportResolver(assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!Libraries.Contains(libraryName))
        {
            return IntPtr.Zero;
        }

        if (TargetLibraryPointer != IntPtr.Zero)
        {
            return TargetLibraryPointer;
        }

        var osDependentLibraryName = GetOsDependentLibraryName();
        TargetLibraryPointer = NativeLibrary.Load(osDependentLibraryName, assembly, searchPath);
        return TargetLibraryPointer;
    }
    
    private static string GetOsDependentLibraryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return WindowsLibraryName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OsxLibraryName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return LinuxLibraryName;

        throw new System.Exception("Unknown platform");
    }
}