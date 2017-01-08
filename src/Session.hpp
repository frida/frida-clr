#pragma once

#using <WindowsBase.dll>

#include <frida-core.h>
#include <msclr/gcroot.h>

using namespace System;
using System::Windows::Threading::Dispatcher;

namespace Frida
{
  ref class Script;

  public ref class Session
  {
  internal:
    Session (FridaSession * handle, Dispatcher ^ dispatcher);
  public:
    ~Session ();
  protected:
    !Session ();

  public:
    event EventHandler ^ Detached;

    property unsigned int Pid { unsigned int get (); }

    void Detach ();
    Script ^ CreateScript (String ^ source);
    Script ^ CreateScript (String ^ name, String ^ source);
    void EnableDebugger ();
    void EnableDebugger (UInt16 port);
    void DisableDebugger ();
    void EnableJit ();

  internal:
    void OnDetached (Object ^ sender, EventArgs ^ e);

  private:
    FridaSession * handle;
    msclr::gcroot<Session ^> * selfHandle;

    Dispatcher ^ dispatcher;
    EventHandler ^ onDetachedHandler;
  };
}
