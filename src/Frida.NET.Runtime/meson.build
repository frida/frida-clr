fs = import('fs')

if frida_core_dep.type_name() == 'internal'
  frida_core_subprj = subproject('frida-core')
  frida_core_lib = frida_core_subprj.get_variable('core_public_lib')
else
  frida_core_lib = frida_core_dep
endif

host_system = host_machine.system()

if host_system == 'windows'
  vs_module_defs = 'Frida.def'
else
  vs_module_defs = ''
endif

wrapper_sources = files('frida-wrapper.c')

frida_dll = shared_library(
  'Frida',
  wrapper_sources,
  dependencies: [ frida_core_dep ],
  vs_module_defs: vs_module_defs
)

library_runtime_output = 'Frida.NET.Runtime.' + meson.project_version() + '.nupkg'

host_cpu = host_machine.cpu_family()
host_os = host_machine.system()

dotnet_platform = ''

if host_os == 'linux'
  if host_cpu == 'x86_64'
    dotnet_platform = 'linux-x86_64'
  elif host_cpu == 'x86'
    dotnet_platform = 'linux-x86'
  elif host_cpu == 'arm'
    dotnet_platform = 'linux-armhf'
  elif host_cpu == 'aarch64'
    dotnet_platform = 'linux-arm64'
  elif host_cpu == 'mips64'
    dotnet_platform = 'linux-mips64'
  endif
elif host_os == 'windows'
  dotnet_platform = host_cpu == 'x86' ? 'win-x86' : 'win-x86_64'
elif host_os == 'darwin'
  dotnet_platform = host_cpu == 'x86_64' ? 'macos-x86_64' : 'macos-arm64'
endif

build_library_runtime = custom_target(
  'build-library-runtime',
  input: [
    fs.copyfile('Frida.NET.Runtime.csproj', 'Frida.NET.Runtime.csproj')
  ],
  output: library_runtime_output,
  depends: [frida_dll],
  command: [
    'dotnet',
    'pack',
    '-o', meson.current_build_dir(),
    '-c', 'Release',
    '-p:VersionPrefix=' + meson.project_version(),
    '-p:Platform=' + dotnet_platform,
    '@INPUT0@'
  ]
)