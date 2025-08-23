// ReSharper disable RedundantNameQualifier
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Frida.Module;

internal static class Initializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Importer.RegisterAsDllImportResolver(typeof(GLib.Module).Assembly);
        Importer.RegisterAsDllImportResolver(typeof(GObject.Module).Assembly);
        Importer.RegisterAsDllImportResolver(typeof(Gio.Module).Assembly);
        Importer.RegisterAsDllImportResolver(typeof(Frida.Internal.ImportResolver).Assembly);
        
        InitializeModule("GLib.Internal.TypeRegistration", typeof(GLib.Module).Assembly);
        InitializeModule("GObject.Internal.TypeRegistration", typeof(GObject.Module).Assembly);
        InitializeModule("Gio.Internal.TypeRegistration", typeof(Gio.Module).Assembly);
        
        Frida.Internal.TypeRegistration.RegisterTypes();
    }

    private static void InitializeModule(string typeRegistrationName, Assembly assembly)
    {
        var typeRegistration = assembly.GetType(typeRegistrationName);
        if (typeRegistration == null)
        {
            return;
        }
        
        var registerMethod = typeRegistration.GetMethod("RegisterTypes", BindingFlags.Static | BindingFlags.NonPublic);
        if (registerMethod == null)
        {
            return;
        }

        registerMethod.Invoke(null, null);
    }
}