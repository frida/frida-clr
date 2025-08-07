fs = import('fs')

if frida_core_dep.type_name() == 'internal'
  frida_core_subprj = subproject('frida-core')
  frida_gir = frida_core_subprj.get_variable('core_public_gir')
  girdir = fs.parent(frida_gir)
else
  girdir = frida_core_dep.get_variable('frida_girdir')
  frida_gir = girdir / 'Frida-1.0.gir'
endif

generate_bindings = custom_target('generate-bindings',
  input: [
    frida_gir
  ],
  output: ['Frida-1.0'],
  command: [
    girtool,
    'generate',
    '--search-path-linux', girdir,
    '--search-path-macos', girdir,
    '--search-path-windows', girdir,
    '--output', meson.current_build_dir(),
    frida_gir.full_path()
  ]
)

copy_bindings = custom_target('copy-bindings',
  input: [
    files('helper.py')
  ],
  output: 'copybindings.stamp',
  depends: [generate_bindings],
  command: [
    find_program('python3'),
    files('helper.py'),
    '--bindings-in', generate_bindings,
    '--bindings-out', meson.current_source_dir(),
    '--dummy-output-file', '@OUTPUT@',
  ]
)

build_library_bindings = custom_target(
  'build-library-bindings',
  input: [
    files(
      'Frida.NET.Bindings.csproj'
    )
  ],
  output: 'Frida.NET.Bindings.1.0.0.nupkg',
  depends: [copy_bindings],
  command: [
    'dotnet',
    'pack',
    '-o', meson.current_build_dir(),
    '-c', 'Release',
    '-p:VersionPrefix=' + meson.project_version(),
    files('Frida.NET.Bindings.csproj')
  ]
)