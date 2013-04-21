#pragma once

namespace Frida
{
  class Application
  {
  public:
    static void ref ();
    static void unref ();

  private:
    static volatile int refCount;
  };
}