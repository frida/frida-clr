#include "Marshal.hpp"

#include <msclr/marshal.h>

using namespace System;
using namespace System::Windows;
using namespace System::Windows::Media;
using namespace System::Windows::Media::Imaging;

namespace Frida
{
  System::String ^
  Marshal::UTF8CStringToClrString (const char * str)
  {
    wchar_t * strUtf16 = reinterpret_cast<wchar_t *> (g_utf8_to_utf16 (str, -1, NULL, NULL, NULL));
    System::String ^ result = gcnew System::String (strUtf16);
    g_free (strUtf16);
    return result;
  }

  char *
  Marshal::ClrStringToUTF8CString (System::String ^ str)
  {
    msclr::interop::marshal_context ^ context = gcnew msclr::interop::marshal_context ();
    const wchar_t * strUtf16 = context->marshal_as<const wchar_t *> (str);
    gchar * strUtf8 = g_utf16_to_utf8 (reinterpret_cast<const gunichar2 *> (strUtf16), -1, NULL, NULL, NULL);
    delete context;
    return strUtf8;
  }

  gchar **
  Marshal::ClrStringArrayToUTF8CStringVector (array<System::String ^> ^ arr)
  {
    if (arr == nullptr)
      return NULL;
    gchar ** result = g_new0 (gchar *, arr->Length + 1);
    for (int i = 0; i != arr->Length; i++)
      result[i] = Marshal::ClrStringToUTF8CString (arr[i]);
    return result;
  }

  array<unsigned char> ^
  Marshal::ByteArrayToClrArray (const guint8 * data, gint length)
  {
    if (length == 0)
      return nullptr;
    array<unsigned char> ^ result = gcnew array<unsigned char> (length);
    pin_ptr<byte> resultStart = &result[0];
    memcpy (resultStart, data, length);
    return result;
  }

  ImageSource ^
  Marshal::FridaIconToImageSource (FridaIcon * icon)
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

  void
  Marshal::ThrowGErrorIfSet (GError ** error)
  {
    if (*error == NULL)
      return;
    System::String ^ message = UTF8CStringToClrString ((*error)->message);
    g_clear_error (error);
    throw gcnew System::Exception (message);
  }
}