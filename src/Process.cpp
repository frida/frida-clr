#include "Process.hpp"

#include "Application.hpp"
#include "Marshal.hpp"

using namespace System::Windows;
using namespace System::Windows::Media::Imaging;

namespace Frida
{
  static ImageSource ^ FridaIconToImageSource (FridaIcon * icon);

  Process::Process (FridaProcess * handle)
    : handle (handle)
  {
    Application::ref ();
  }

  Process::~Process ()
  {
    if (handle == NULL)
      return;

    this->!Process ();
  }

  Process::!Process ()
  {
    if (handle != NULL)
    {
      g_object_unref (handle);
      handle = NULL;

      Application::unref ();
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
    return FridaIconToImageSource (frida_process_get_small_icon (handle));
  }

  ImageSource ^
  Process::LargeIcon::get ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Process");
    return FridaIconToImageSource (frida_process_get_large_icon (handle));
  }

  String ^
  Process::ToString ()
  {
    if (handle == NULL)
      throw gcnew ObjectDisposedException ("Process");
    return String::Format ("Pid: {0}, Name: \"{1}\"", Pid, Name);
  }

  static ImageSource ^
  FridaIconToImageSource (FridaIcon * icon)
  {
    if (icon == NULL)
      return nullptr;

    gint width = frida_icon_get_width (icon);
    gint height = frida_icon_get_height (icon);
    gint rowstride = frida_icon_get_rowstride (icon);
    gint pixelsLength;
    guint8 * pixelsRgba = frida_icon_get_pixels (icon, &pixelsLength);

    guint8 * pixelsBgra = static_cast<guint8 *> (g_memdup (pixelsRgba, pixelsLength));
    guint8 * rowStart = pixelsBgra;
    for (gint row = 0; row != height; row++)
    {
      guint32 * pixel = reinterpret_cast<guint32 *> (rowStart); 
      for (gint col = 0; col != width; col++)
      {
        *pixel = ((*pixel & 0x000000ff) << 16) |
                 ((*pixel & 0x0000ff00) <<  0) |
                 ((*pixel & 0x00ff0000) >> 16) |
                 ((*pixel & 0xff000000) >>  0);
        pixel++;
      }

      rowStart += rowstride;
    }

    WriteableBitmap ^ bitmap = gcnew WriteableBitmap (width, height, 96, 96, PixelFormats::Pbgra32, nullptr);
    bitmap->WritePixels (Int32Rect (0, 0, width, height), IntPtr (pixelsBgra), pixelsLength, rowstride, 0, 0);

    g_free (pixelsBgra);

    return bitmap;
  }
}