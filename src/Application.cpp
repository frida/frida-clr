#include "Application.hpp"

#include <frida-core.h>

namespace Frida
{
  volatile int Application::refCount = 0;

  void Application::ref ()
  {
    g_atomic_int_inc (&refCount);
    frida_init ();
  }

  void Application::unref ()
  {
    if (g_atomic_int_dec_and_test (&refCount))
      frida_deinit ();
  }

  class Assembly
  {
  public:
    Assembly ()
    {
      Application::ref ();
    }

    ~Assembly ()
    {
      Application::unref ();
    }
  };
  static Assembly assembly;
}