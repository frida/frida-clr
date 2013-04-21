#pragma once

#using <PresentationCore.dll>
#using <WindowsBase.dll>

#include <frida-core.h>

using namespace System;
using namespace System::Windows::Media;

namespace Frida
{
  public ref class Process
  {
  internal:
    Process (FridaProcess * handle);
  public:
    ~Process ();
  protected:
    !Process ();

  public:
    property unsigned int Pid { unsigned int get (); }
    property String ^ Name { String ^ get (); }
    property ImageSource ^ SmallIcon { ImageSource ^ get (); }
    property ImageSource ^ LargeIcon { ImageSource ^ get (); }

    virtual String ^ ToString () override;

  private:
    FridaProcess * handle;

    ImageSource ^ smallIcon;
    ImageSource ^ largeIcon;
  };
}