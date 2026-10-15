import argparse
import os
import subprocess
from os import PathLike
from typing import Optional


def main(gir_scripts: PathLike,
         gir_src: PathLike,
         dummy_output_file: Optional[PathLike]) -> None:
    # These generators run on the build machine, so they must target its native
    # architecture. When cross-compiling for a Windows x86 host, frida's
    # environment carries Platform=x86, which makes MSBuild emit an x86 apphost
    # that cannot load the x64 hostfxr. Drop it so child builds stay native.
    os.environ.pop('Platform', None)

    subprocess.run(['dotnet', 'fsi', 'GenerateLibs.fsx'], cwd=gir_scripts, check=True)
    subprocess.run(['dotnet', 'build', 'GirCore.Libs.slnf'], cwd=gir_src, check=True)

    if dummy_output_file:
        with open(dummy_output_file, 'w', encoding='utf-8') as f:
            f.write('dummy file for custom_target output')

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--gir-scripts', required=True)
    parser.add_argument('--gir-src', required=True)
    parser.add_argument('--dummy-output-file', type=str)
    args = parser.parse_args()

    main(gir_scripts=args.gir_scripts,
         gir_src=args.gir_src,
         dummy_output_file=args.dummy_output_file)
