#include "Process.hpp"

#include "Marshal.hpp"
#include "Runtime.hpp"

namespace Frida
{
  Process::Process (FridaProcess * handle)
    : handle (handle),
      smallIcon (nullptr),
      largeIcon (nullptr)
  {
    Runtime::Ref ();
  }

  Process::~Process ()
  {
    if (handle == NULL)
      return;

    if (largeIcon != nullptr)
    {
      delete largeIcon;
      largeIcon = nullptr;
    }
    if (smallIcon != nullptr)
    {
      delete smallIcon;
      smallIcon = nullptr;
    }

    this->!Process ();
  }

  Process::!Process ()
  {
    if (handle != NULL)
    {
      g_object_unref (handle);
      handle = NULL;

      Runtime::Unref ();
    }
  }

  unsigned int
  Process::Pid::get ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Process");
    return frida_process_get_pid (handle);
  }

  String ^
  Process::Name::get ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Process");
    return Marshal::UTF8CStringToClrString (frida_process_get_name (handle));
  }

  ImageSource ^
  Process::SmallIcon::get ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Process");
    if (smallIcon == nullptr)
      smallIcon = Marshal::FridaIconToImageSource (frida_process_get_small_icon (handle));
    return smallIcon;
  }

  ImageSource ^
  Process::LargeIcon::get ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Process");
    if (largeIcon == nullptr)
      largeIcon = Marshal::FridaIconToImageSource (frida_process_get_large_icon (handle));
    return largeIcon;
  }

  String ^
  Process::ToString ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Process");
    return String::Format ("Pid: {0}, Name: \"{1}\"", Pid, Name);
  }
}