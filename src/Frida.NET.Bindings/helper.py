import argparse
import glob
import os
import subprocess
from os import PathLike
from typing import Optional


def main(bindings_in: PathLike,
         bindings_out: PathLike,
         dummy_output_file: Optional[PathLike]) -> None:
    for binding_file in glob.glob(os.path.join(bindings_in, '**/*.cs'), recursive=True):
        path_rel = os.path.relpath(binding_file, bindings_in)
        path_out = os.path.join(bindings_out, path_rel)

        os.makedirs(os.path.dirname(path_out), exist_ok=True)

        with open(binding_file, 'r', encoding='utf-8') as f:
            with open(path_out, 'w', encoding='utf-8') as f_out:
                content = f.read()
                content = content.replace('public class', 'internal class')
                content = content.replace('public partial class', 'internal partial class')
                content = content.replace('public partial struct', 'internal partial struct')
                content = content.replace('public partial interface', 'internal partial interface')
                content = content.replace('public sealed partial class', 'internal sealed partial class')
                content = content.replace('public delegate', 'internal delegate')
                f_out.write(content)

    if dummy_output_file:
        with open(dummy_output_file, 'w', encoding='utf-8') as f:
            f.write('dummy file for custom_target output')

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--bindings-in', required=True)
    parser.add_argument('--bindings-out', required=True)
    parser.add_argument('--dummy-output-file', type=str)
    args = parser.parse_args()

    main(bindings_in=args.bindings_in,
         bindings_out=args.bindings_out,
         dummy_output_file=args.dummy_output_file)
