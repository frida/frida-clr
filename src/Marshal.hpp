#pragma once

#include <glib.h>

namespace Frida
{
  private ref class Marshal
  {
  public:
    static System::String ^ UTF8CStringToClrString (const char * str);
    static char * ClrStringToUTF8CString (System::String ^ str);
    static gchar ** ClrStringArrayToUTF8CStringVector (array<System::String ^> ^ arr);
    static array<unsigned char> ^ ByteArrayToClrArray (const guint8 * data, gint length);

    static void ThrowGErrorIfSet (GError ** error);
  };
}