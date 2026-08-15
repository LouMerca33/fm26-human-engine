#!/bin/sh
# Builds libversion_shim.dylib for x86_64 (required: FM26 runs translated
# under Rosetta 2 via `arch -x86_64 ./run_bepinex.sh`, so the interposer
# dylib injected into the same process must match that architecture).
set -e
cd "$(dirname "$0")"
clang -arch x86_64 -dynamiclib -O2 -Wall -Wextra \
    -o libversion_shim.dylib \
    version_shim.c
echo "Built $(pwd)/libversion_shim.dylib"
file libversion_shim.dylib
