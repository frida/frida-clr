project('frida-net', 'c',
  version: run_command(find_program('python3'), files('tools' / 'detect-version.py'),
    capture: true,
    check: true).stdout().strip(),
  meson_version: '>=1.4.0',
  default_options: [],
)

frida_core_dep = dependency('frida-core-1.0', default_options: [
  'frida_version=' + meson.project_version()
])

subdir('tools/girtool')
subdir('src')