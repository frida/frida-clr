fs = import('fs')

if frida_core_dep.type_name() == 'internal'
  frida_core_subprj = subproject('frida-core')
  frida_core_lib = frida_core_subprj.get_variable('core_public_lib')
else
  frida_core_lib = frida_core_dep
endif

host_system = host_machine.system()

wrapper_sources = files('frida-wrapper.c')

frida_dll_kwargs = {}
if host_system == 'windows'
  frida_dll_kwargs += {'vs_module_defs': 'Frida.def'}
endif

frida_dll = shared_library(
  'Frida',
  wrapper_sources,
  name_prefix: '',
  dependencies: [ frida_core_dep ],
  kwargs: frida_dll_kwargs
)

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

rid = {
  'win-x86': 'win-x86',
  'win-x86_64': 'win-x64',
  'macos-x86_64': 'osx-x64',
  'macos-arm64': 'osx-arm64',
  'linux-x86': 'linux-x86',
  'linux-x86_64': 'linux-x64',
  'linux-armhf': 'linux-arm32',
  'linux-arm64': 'linux-arm64',
  'linux-mips64': 'linux-mips64',
}[dotnet_platform]
library_runtime_output = 'Frida.NET.Runtime.' + rid + '.' + meson.project_version() + '.nupkg'

build_library_runtime = custom_target(
  'build-runtime',
  build_by_default: true,
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