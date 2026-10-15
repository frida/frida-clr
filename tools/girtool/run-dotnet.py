import os
import subprocess
import sys

# The gir code generators run on the build machine, so they must target its
# native architecture. When cross-compiling for a Windows x86 host, frida's
# environment carries Platform=x86, which makes MSBuild emit an x86 build that
# clashes with the inferred x64 RuntimeIdentifier. Drop it before invoking
# dotnet so the SDK builds for the build machine. Pop it from os.environ
# directly, whose keys are case-insensitive on Windows, where it lands as
# PLATFORM.
os.environ.pop("Platform", None)

sys.exit(subprocess.run(["dotnet", *sys.argv[1:]]).returncode)
