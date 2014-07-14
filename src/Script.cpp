#include "Script.hpp"

#include "Marshal.hpp"
#include "Runtime.hpp"

using System::Windows::Threading::DispatcherPriority;

namespace Frida
{
  static void OnScriptMessage (FridaScript * script, const gchar * message, const guint8 * data, gint data_length, gpointer user_data);

  Script::Script (FridaScript * handle, Dispatcher ^ dispatcher)
    : handle (handle),
      dispatcher (dispatcher)
  {
    Runtime::Ref ();

    selfHandle = new msclr::gcroot<Script ^> (this);
    onMessageHandler = gcnew ScriptMessageHandler (this, &Script::OnMessage);
    g_signal_connect (handle, "message", G_CALLBACK (OnScriptMessage), selfHandle);
  }

  Script::~Script ()
  {
    if (handle == NULL)
      return;

    g_signal_handlers_disconnect_by_func (handle, OnScriptMessage, selfHandle);
    delete selfHandle;
    selfHandle = NULL;

    this->!Script ();
  }

  Script::!Script ()
  {
    if (handle != NULL)
    {
      g_object_unref (handle);
      handle = NULL;

      Runtime::Unref ();
    }
  }

  void
  Script::Load ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Script");

    GError * error = NULL;
    frida_script_load_sync (handle, &error);
    Marshal::ThrowGErrorIfSet (&error);
  }

  void
  Script::Unload ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Script");

    GError * error = NULL;
    frida_script_unload_sync (handle, &error);
    Marshal::ThrowGErrorIfSet (&error);
  }

  void
  Script::PostMessage (String ^ message)
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Script");

    GError * error = NULL;
    gchar * messageUtf8 = Marshal::ClrStringToUTF8CString (message);
    frida_script_post_message_sync (handle, messageUtf8, &error);
    g_free (messageUtf8);
    Marshal::ThrowGErrorIfSet (&error);
  }

  void
  Script::OnMessage (Object ^ sender, ScriptMessageEventArgs ^ e)
  {
    if (dispatcher->CheckAccess ())
      Message (sender, e);
    else
      dispatcher->BeginInvoke (DispatcherPriority::Normal, onMessageHandler, sender, e);
  }

  static void
  OnScriptMessage (FridaScript * script, const gchar * message, const guint8 * data, gint data_length, gpointer user_data)
  {
    (void) script;

    msclr::gcroot<Script ^> * wrapper = static_cast<msclr::gcroot<Script ^> *> (user_data);
    ScriptMessageEventArgs ^ e = gcnew ScriptMessageEventArgs (
        Marshal::UTF8CStringToClrString (message),
        Marshal::ByteArrayToClrArray (data, data_length));
   (*wrapper)->OnMessage (*wrapper, e);
  }
}