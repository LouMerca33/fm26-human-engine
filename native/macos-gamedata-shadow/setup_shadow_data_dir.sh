#!/bin/sh
# setup_shadow_data_dir.sh
#
# Root cause (see STATUS.md, "Chantier BepInEx" section, for the full
# writeup): on macOS, BepInEx.Core.Paths.SetExecutablePath() computes
#   GameDataPath = Path.Combine(GameRootPath, ProcessName + "_Data")
# ...which is the Windows/Linux Unity convention (e.g. "MyGame_Data/" next
# to a bare "MyGame.exe"). It is *not* adjusted for the macOS .app bundle
# layout, where the actual data lives in
#   <GameRoot>/<name>.app/Contents/Resources/Data/
# Since "<GameRoot>/Football Manager 26_Data" never exists, every
# File.Exists() check BepInEx.Unity.Common.UnityInfo.DetermineVersion()
# makes (globalgamemanagers / data.unity3d / mainData) fails immediately,
# without ever reading the game's actual Unity version -- it silently
# falls back to UnityVersion 0.0.0a0, which cascades into a 404 downloading
# Il2CppInterop's base libraries and a fatal
# "Il2CppInterop.Runtime.UnityVersionHandler: No handler" exception in the
# Preloader, before any plugin loads.
#
# (There is a second, independent issue on top of this: FM26 embeds a
# non-standard Unity version string, "6000.0.52f1-fm26-05f1" -- Sports
# Interactive's custom build suffix -- which UnityVersion.Parse() from
# AssetRipper.Primitives also rejects. Both problems have to be fixed for
# version detection to succeed.)
#
# Fix: create the "<ProcessName>_Data" directory BepInEx expects as a
# *sibling* of the .app bundle (this is a completely ordinary, freely
# writable location in the normal Steam game folder -- NOT inside
# fm.app/Contents/..., which is blocked by macOS's "App Management"
# protection). Populate it with symlinks mirroring the real Data folder,
# except for globalgamemanagers, which gets a real copy with exactly one
# byte patched: the '-' at absolute offset 59 (0x3B), right after
# "6000.0.52f1", turned into a NUL terminator. That truncates the string
# BepInEx reads from "6000.0.52f1-fm26-05f1" to "6000.0.52f1", which
# UnityVersion.Parse() accepts. The real file on disk (inside fm.app) is
# never touched.
#
# Idempotent: safe to re-run any time (e.g. after a game update changes
# globalgamemanagers -- just re-run this script to refresh the shadow
# copy and re-verify the offset/byte before patching).
#
# Usage: ./setup_shadow_data_dir.sh ["/path/to/Football Manager 26"]
# Defaults to the standard Steam install location on this machine.

set -e

GAMEDIR="${1:-$HOME/Library/Application Support/Steam/steamapps/common/Football Manager 26}"
APP="$GAMEDIR/fm.app"
REALDATA="$APP/Contents/Resources/Data"
PROCESS_NAME="Football Manager 26"   # Path.GetFileNameWithoutExtension(Paths.ExecutablePath)
SHADOWDATA="$GAMEDIR/${PROCESS_NAME}_Data"

PATCH_OFFSET=59   # 0x3B
ORIG_BYTE_HEX="2d"     # '-'
PATCHED_BYTE_HEX="00"  # NUL

if [ ! -d "$REALDATA" ]; then
    echo "error: real Data folder not found at: $REALDATA" >&2
    echo "(pass the game root directory as \$1 if it's not at the default Steam path)" >&2
    exit 1
fi

echo "Game root:   $GAMEDIR"
echo "Real Data:   $REALDATA"
echo "Shadow Data: $SHADOWDATA"

mkdir -p "$SHADOWDATA"

# Symlink everything except globalgamemanagers, which needs a patched copy.
for item in "$REALDATA"/*; do
    name=$(basename "$item")
    if [ "$name" = "globalgamemanagers" ]; then
        continue
    fi
    # `ln -sf` alone is not reliably idempotent on macOS when the existing
    # link points at a directory (it can end up creating the new link
    # *inside* that directory instead of replacing it) -- remove first.
    rm -f "$SHADOWDATA/$name"
    ln -s "$item" "$SHADOWDATA/$name"
done

# Real (patched) copy of globalgamemanagers.
cp "$REALDATA/globalgamemanagers" "$SHADOWDATA/globalgamemanagers"

# Verify the byte we're about to patch is exactly what we expect before
# touching it -- if a game update ever changes this layout, fail loudly
# instead of silently corrupting an unrelated byte.
actual_byte=$(xxd -p -s "$PATCH_OFFSET" -l 1 "$SHADOWDATA/globalgamemanagers")
if [ "$actual_byte" != "$ORIG_BYTE_HEX" ]; then
    echo "error: byte at offset $PATCH_OFFSET is 0x$actual_byte, expected 0x$ORIG_BYTE_HEX ('-')." >&2
    echo "The game's globalgamemanagers layout may have changed (game update?)." >&2
    echo "Re-check the version string offset by hand (xxd -l 32 -s 48 \"$REALDATA/globalgamemanagers\")" >&2
    echo "before adjusting PATCH_OFFSET/ORIG_BYTE_HEX in this script." >&2
    rm -f "$SHADOWDATA/globalgamemanagers"
    exit 1
fi

dd if=/dev/zero of="$SHADOWDATA/globalgamemanagers" bs=1 seek="$PATCH_OFFSET" count=1 conv=notrunc 2>/dev/null

echo "Patched byte at offset $PATCH_OFFSET: 0x$ORIG_BYTE_HEX -> 0x$PATCHED_BYTE_HEX"
echo "Resulting version string:"
xxd -l 16 -s 48 "$SHADOWDATA/globalgamemanagers"
echo "Done."
