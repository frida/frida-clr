host = host_machine
exe_suffix = host.system() == 'windows' ? '.exe' : ''

girinit = custom_target('initialize-gir-core',
  input: [
    files('initialize-gir-core.py')
  ],
  output: 'girinit.stamp',
  command: [
    find_program('python3'),
    files('initialize-gir-core.py'),
    '--gir-scripts', meson.project_source_root() / 'gir.core' / 'scripts',
    '--gir-src', meson.project_source_root() / 'gir.core' / 'src',
    '--dummy-output-file', '@OUTPUT@',
  ]
)

girtool = custom_target('build-gir-tool',
  output: 'GirTool' + exe_suffix,
  depends: [girinit],
  command: [
    'dotnet',
    'publish',
    '-o', meson.current_build_dir(),
    '-c', 'Release',
    '-p:PublishSingleFile=true',
    meson.project_source_root() / 'gir.core' / 'src' / 'Generation' / 'GirTool' / 'GirTool.csproj'
  ]
)