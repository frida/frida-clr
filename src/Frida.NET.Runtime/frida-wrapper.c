#include <frida-core.h>

#ifdef _WIN32

#include <Windows.h>
#include <stdlib.h>

BOOL WINAPI DllMain(
    HINSTANCE hinstDLL,
    DWORD fdwReason,
    LPVOID lpvReserved)
{
    switch (fdwReason)
    {
        case DLL_PROCESS_ATTACH:
            frida_init();
            break;

        case DLL_PROCESS_DETACH:
            frida_deinit();
            break;
    }

    return TRUE;
}

#else

__attribute__((constructor))
static void frida_init_wrapper(void)
{
    frida_init();
}

__attribute__((destructor))
static void frida_deinit_wrapper(void)
{
    frida_deinit();
}

#endif
