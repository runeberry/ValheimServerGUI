#!/usr/bin/env bash
# Replace the signed Windows exe in ASSET_DIR with a zip holding it under a stable name, so the
# download carries the version while the extracted program is always ValheimServerGUI.exe. Runs
# after signing; the Linux artifacts are attached as they are.
#
# Env: ASSET_DIR.
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }

[ -n "${ASSET_DIR:-}" ] || die "ASSET_DIR is not set."

exes=("$ASSET_DIR"/*-win-x64.exe)
[ "${#exes[@]}" -eq 1 ] && [ -f "${exes[0]}" ] || die "expected exactly one *-win-x64.exe in ${ASSET_DIR}."
exe="${exes[0]}"
zip_path="${exe%.exe}.zip"

stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
cp "$exe" "$stage/ValheimServerGUI.exe"
rm -f "$zip_path"
zip -j -X "$zip_path" "$stage/ValheimServerGUI.exe"
rm "$exe"

unzip -l "$zip_path"
