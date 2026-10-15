fs = import('fs')

exe_suffix = host_machine.system() == 'windows' ? '.exe' : ''
if host_machine.system() == 'windows'
  gir_files_platform = 'windows'
elif host_machine.system() == 'darwin'
  gir_files_platform = 'macos'
else
  gir_files_platform = 'linux'
endif
gir_files_dir = meson.project_source_root() / 'gir.core' / 'ext' / 'gir-files' / gir_files_platform

clrgen = custom_target('build-clr-generator',
  output: 'Frida.Codegen' + exe_suffix,
  depends: [girinit, girtool],
  command: [
    python,
    run_dotnet,
    'publish',
    '-o', meson.current_build_dir(),
    '-c', 'Release',
    '-p:PublishSingleFile=true',
    meson.project_source_root() / 'tools' / 'clrgen' / 'Frida.Codegen.csproj'
  ]
)

generate_wrappers = custom_target('generate-wrappers',
  input: [
    frida_gir
  ],
  output: 'generatewrappers.stamp',
  depends: [clrgen, copy_bindings],
  command: [
    clrgen,
    girdir,
    '--all', meson.current_source_dir() / 'Generated',
    '--search', gir_files_dir,
    '--stamp', '@OUTPUT@',
  ]
)

build_library = custom_target(
  'build-library',
  build_by_default: true,
  input: [
    files(
      'Frida.NET.csproj'
    )
  ],
  output: 'Frida.NET.' + meson.project_version() + '.nupkg',
  depends: [build_library_bindings, generate_wrappers],
  command: [
    'dotnet',
    'pack',
    '-o', meson.current_build_dir(),
    '-c', 'Release',
    '-p:VersionPrefix=' + meson.project_version(),
    files('Frida.NET.csproj')
  ]
)
