project('frida-clr', 'cpp',
  version: '0.0.0',
  default_options: ['b_vscrt=md'],
)

host_msvc_platform = host_machine.cpu_family() == 'x86_64' ? 'x64' : 'x86'

detect_result = run_command(import('python').find_installation(), files('detect-netfx.py'), check: false)
if detect_result.returncode() != 0
  error(detect_result.stderr().strip())
endif
locations = detect_result.stdout().strip().split('\n')
netfx_runtime = locations[0]
netfx_sdk = locations[1]

frida_core_dep = dependency('frida-core-1.0', default_options: [
  'compiler_backend=disabled',
])

subdir('src')
