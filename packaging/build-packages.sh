#!/usr/bin/env bash
#
# build-packages.sh — runs INSIDE the packaging container (see scripts/package.sh + packaging/Dockerfile).
# Produces the distributable, framework-dependent artifacts (they require the .NET 10 desktop runtime on
# the target machine) into $OUT_DIR:
#   * ValheimServerGUI-<ver>-win-x64.exe        (single-file Windows executable)
#   * ValheimServerGUI-<ver>-linux-x64.tar.gz   (Linux publish + .desktop + icon)
#   * ValheimServerGUI-<ver>-x86_64.AppImage    (portable Linux app)
#
# Set SELF_CONTAINED=true to bundle the .NET runtime into every artifact instead (needed if you want the
# AppImage to run without .NET installed — see the note in scripts/package.sh / CLAUDE.local.md).

set -euo pipefail

ROOT="${1:-/src}"
OUT="${OUT_DIR:-$ROOT/dist}"
SELF_CONTAINED="${SELF_CONTAINED:-false}"

APP_CSPROJ="$ROOT/src/ValheimServerGUI.App/ValheimServerGUI.App.csproj"
CORE_CSPROJ="$ROOT/src/ValheimServerGUI.Core/ValheimServerGUI.Core.csproj"
ICON="$ROOT/src/ValheimServerGUI.App/Assets/vsg_logo_256.png"

VERSION="$(grep -oP '<Version>\K[^<]+' "$CORE_CSPROJ" | head -1)"
[ -n "$VERSION" ] || { echo "!! could not read <Version> from Core.csproj" >&2; exit 1; }

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
mkdir -p "$OUT"

echo ">> Packaging ValheimServerGUI $VERSION (self-contained=$SELF_CONTAINED)"

publish() { # <rid> <out-dir> [extra msbuild args...]
    dotnet publish "$APP_CSPROJ" -c Release -r "$1" --self-contained "$SELF_CONTAINED" \
        -p:Version="$VERSION" --nologo -o "$2" "${@:3}"
}

write_desktop() { # <exec> <out-path>
    cat > "$2" <<EOF
[Desktop Entry]
Type=Application
Name=Valheim Server GUI
Comment=Manage a Valheim dedicated server
Exec=$1
Icon=valheim-server-gui
Terminal=false
Categories=Game;Utility;
EOF
}

# ---- Windows single-file .exe ---------------------------------------------------------------
echo ">> [1/3] Windows win-x64 single-file"
publish win-x64 "$work/win" -p:PublishSingleFile=true
cp "$work/win/ValheimServerGUI.App.exe" "$OUT/ValheimServerGUI-$VERSION-win-x64.exe"

# ---- Linux publish (shared by the tarball + AppImage) ---------------------------------------
echo ">> [2/3] Linux linux-x64 tarball"
publish linux-x64 "$work/linux"
chmod +x "$work/linux/ValheimServerGUI.App"

tarname="ValheimServerGUI-$VERSION-linux-x64"
tardir="$work/tar/$tarname"
mkdir -p "$tardir"
cp -r "$work/linux/." "$tardir/"
cp "$ICON" "$tardir/valheim-server-gui.png"
write_desktop "ValheimServerGUI.App" "$tardir/ValheimServerGUI.desktop"
tar czf "$OUT/$tarname.tar.gz" -C "$work/tar" "$tarname"

# ---- AppImage -------------------------------------------------------------------------------
echo ">> [3/3] Linux AppImage"
appdir="$work/AppDir"
mkdir -p "$appdir/usr/bin"
cp -r "$work/linux/." "$appdir/usr/bin/"
cp "$ICON" "$appdir/valheim-server-gui.png"
write_desktop "ValheimServerGUI.App" "$appdir/valheim-server-gui.desktop"
cat > "$appdir/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/bin/ValheimServerGUI.App" "$@"
EOF
chmod +x "$appdir/AppRun"
ARCH=x86_64 appimagetool "$appdir" "$OUT/ValheimServerGUI-$VERSION-x86_64.AppImage"

echo ">> Done. Artifacts in $OUT:"
ls -la "$OUT"
