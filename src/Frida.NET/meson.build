fs = import('fs')

build_library = custom_target(
  'build-library',
  input: [
    files(
      'Frida.NET.csproj'
    )
  ],
  output: 'Frida.NET.1.0.0.nupkg',
  depends: [build_library_bindings],
  command: [
    'dotnet',
    'pack',
    '-o', meson.current_build_dir(),
    '-c', 'Release',
    '-p:VersionPrefix=' + meson.project_version(),
    files('Frida.NET.csproj')
  ]
)