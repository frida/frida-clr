#include <frida-core.h>

#ifdef _WIN32

#include <Windows.h>
#include <stdlib.h>

BOOL WINAPI DllMain(
    HINSTANCE hinstDLL,
    DWORD fdwReason,
    LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        frida_init();
    }

    return TRUE;
}

#else

__attribute__((constructor))
static void frida_init_wrapper(void)
{
    frida_init();
}

#endif
