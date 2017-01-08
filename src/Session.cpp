#include "Session.hpp"

#include "Marshal.hpp"
#include "Runtime.hpp"
#include "Script.hpp"

using System::Windows::Threading::DispatcherPriority;

namespace Frida
{
  static void OnSessionDetached (FridaSession * session, gpointer user_data);

  Session::Session (FridaSession * handle, Dispatcher ^ dispatcher)
    : handle (handle),
      dispatcher (dispatcher)
  {
    Runtime::Ref ();

    selfHandle = new msclr::gcroot<Session ^> (this);
    onDetachedHandler = gcnew EventHandler (this, &Session::OnDetached);
    g_signal_connect (handle, "detached", G_CALLBACK (OnSessionDetached), selfHandle);
  }

  Session::~Session ()
  {
    if (handle == NULL)
      return;

    g_signal_handlers_disconnect_by_func (handle, OnSessionDetached, selfHandle);
    delete selfHandle;
    selfHandle = NULL;

    this->!Session ();
  }

  Session::!Session ()
  {
    if (handle != NULL)
    {
      g_object_unref (handle);
      handle = NULL;

      Runtime::Unref ();
    }
  }

  unsigned int
  Session::Pid::get ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Session");
    return frida_session_get_pid (handle);
  }

  void
  Session::Detach ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Session");
    frida_session_detach_sync (handle);
  }

  Script ^
  Session::CreateScript (String ^ source)
  {
    return CreateScript (nullptr, source);
  }

  Script ^
  Session::CreateScript (String ^ name, String ^ source)
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Session");

    GError * error = NULL;
    gchar * nameUtf8 = (name != nullptr) ? Marshal::ClrStringToUTF8CString (name) : NULL;
    gchar * sourceUtf8 = Marshal::ClrStringToUTF8CString (source);
    FridaScript * script = frida_session_create_script_sync (handle, nameUtf8, sourceUtf8, &error);
    g_free (sourceUtf8);
    g_free (nameUtf8);
    Marshal::ThrowGErrorIfSet (&error);

    return gcnew Script (script, dispatcher);
  }

  void
  Session::EnableDebugger ()
  {
    return EnableDebugger (0);
  }

  void
  Session::EnableDebugger (UInt16 port)
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Session");

    GError * error = NULL;
    frida_session_enable_debugger_sync (handle, port, &error);
    Marshal::ThrowGErrorIfSet (&error);
  }

  void
  Session::DisableDebugger ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Session");

    GError * error = NULL;
    frida_session_disable_debugger_sync (handle, &error);
    Marshal::ThrowGErrorIfSet (&error);
  }

  void
  Session::EnableJit ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Session");

    GError * error = NULL;
    frida_session_enable_jit_sync (handle, &error);
    Marshal::ThrowGErrorIfSet (&error);
  }

  void
  Session::OnDetached (Object ^ sender, EventArgs ^ e)
  {
    if (dispatcher->CheckAccess ())
      Detached (sender, e);
    else
      dispatcher->BeginInvoke (DispatcherPriority::Normal, onDetachedHandler, sender, e);
  }

  static void
  OnSessionDetached (FridaSession * session, gpointer user_data)
  {
    (void) session;

    msclr::gcroot<Session ^> * wrapper = static_cast<msclr::gcroot<Session ^> *> (user_data);
    (*wrapper)->OnDetached (*wrapper, EventArgs::Empty);
  }
}
