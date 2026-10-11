fs = import('fs')

exe_suffix = host_machine.system() == 'windows' ? '.exe' : ''
gir_files_platform = host_machine.system() == 'windows' ? 'windows' \
  : (host_machine.system() == 'darwin' ? 'macos' : 'linux')
gir_files_dir = meson.project_source_root() / 'gir.core' / 'ext' / 'gir-files' / gir_files_platform

clrgen = custom_target('build-clr-generator',
  output: 'Frida.Codegen' + exe_suffix,
  depends: [girinit],
  command: [
    'dotnet',
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
  input: [
    files(
      'Frida.NET.csproj'
    )
  ],
  output: 'Frida.NET.1.0.0.nupkg',
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
