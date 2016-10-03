#pragma once

#using <PresentationCore.dll>
#using <WindowsBase.dll>

#include <frida-core.h>

namespace Frida
{
  private ref class Marshal
  {
  public:
    static System::String ^ UTF8CStringToClrString (const char * str);
    static char * ClrStringToUTF8CString (System::String ^ str);
    static gchar ** ClrStringArrayToUTF8CStringVector (array<System::String ^> ^ arr);
    static array<unsigned char> ^ BytesToClrArray (GBytes * bytes);
    static GBytes * ClrByteArrayToBytes (array<unsigned char> ^ arr);
    static System::Windows::Media::ImageSource ^ Marshal::FridaIconToImageSource (FridaIcon * icon);

    static void ThrowGErrorIfSet (GError ** error);
  };
}