#pragma once

#using <WindowsBase.dll>

#include <frida-core.h>
#include <msclr/gcroot.h>

using namespace System;
using System::Windows::Threading::Dispatcher;

namespace Frida
{
  ref class Process;
  ref class Session;

  public ref class Device
  {
  internal:
    Device (FridaDevice * handle, Dispatcher ^ dispatcher);
  public:
    ~Device ();
  protected:
    !Device ();

  public:
    event EventHandler ^ Lost;

    property unsigned int Id { unsigned int get (); }
    property String ^ Name { String ^ get (); }
    property String ^ Kind { String ^ get (); }

    array<Process ^> ^ EnumerateProcesses ();
    unsigned int Spawn (String ^ path, array<String ^> ^ argv, array<String ^> ^ envp);
    void Resume (unsigned int pid);
    Session ^ Attach (unsigned int pid);

    virtual String ^ ToString () override;

  internal:
    void OnLost (Object ^ sender, EventArgs ^ e);

  private:
    FridaDevice * handle;
    msclr::gcroot<Device ^> * selfHandle;

    Dispatcher ^ dispatcher;
    EventHandler ^ onLostHandler;
  };
}