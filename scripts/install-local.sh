#!/usr/bin/env bash
#
# install-local.sh — publish a stable local build of Valheim Server GUI for
# day-to-day dogfooding, decoupled from the repo working tree.
#
# What it does:
#   1. Publishes a Release build of ValheimServerGUI.App to a shared, stable
#      directory (default: /home/developers/Apps/valheim-server-gui) that does
#      NOT change as the repo is edited — your installed copy stays frozen until
#      you re-run this.
#   2. Stages the app icon + a resolved .desktop launcher entry alongside it.
#   3. Installs the launcher into the current user's application menu if that
#      directory is writable; otherwise prints the one command to run.
#
# Re-run this whenever you want your installed copy to catch up to new features.

set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_PROJ="$REPO_DIR/src/ValheimServerGUI.App/ValheimServerGUI.App.csproj"
PKG_DIR="$REPO_DIR/packaging"
ICON_PNG="$REPO_DIR/src/ValheimServerGUI.App/Assets/vsg_logo_256.png"

INSTALL_DIR="${VSG_INSTALL_DIR:-/home/developers/Apps/valheim-server-gui}"
APPHOST="$INSTALL_DIR/ValheimServerGUI.App"
LAUNCHER_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
DESKTOP_NAME="valheim-server-gui.desktop"

echo "==> Valheim Server GUI local install"
echo "    repo:    $REPO_DIR"
echo "    install: $INSTALL_DIR"
echo

# 1. Publish (framework-dependent — the .NET 10 desktop runtime is present system-wide).
# Build intermediates are redirected per-user by Directory.Build.props, so a shared
# working tree never collides across users (MSB3374); -o still places the published app.
echo "==> Publishing Release build..."
rm -rf "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
dotnet publish "$APP_PROJ" -c Release -o "$INSTALL_DIR" --nologo
chmod +x "$APPHOST"

# 2. Stage the app icon alongside the published app.
cp "$ICON_PNG" "$INSTALL_DIR/valheim-server-gui.png"

# 3. Resolve the .desktop template into the install dir.
STAGED_DESKTOP="$INSTALL_DIR/$DESKTOP_NAME"
sed "s#@INSTALL_DIR@#$INSTALL_DIR#g" "$PKG_DIR/$DESKTOP_NAME.in" > "$STAGED_DESKTOP"

echo
echo "==> Published. Launcher entry staged at:"
echo "    $STAGED_DESKTOP"
echo

# 4. Install the launcher if we can; otherwise hand off the command.
if [ -w "$LAUNCHER_DIR" ] 2>/dev/null || { mkdir -p "$LAUNCHER_DIR" 2>/dev/null && [ -w "$LAUNCHER_DIR" ]; }; then
  cp "$STAGED_DESKTOP" "$LAUNCHER_DIR/$DESKTOP_NAME"
  command -v update-desktop-database >/dev/null 2>&1 && \
    update-desktop-database "$LAUNCHER_DIR" 2>/dev/null || true
  echo "==> Launcher installed to $LAUNCHER_DIR"
  echo "    'Valheim Server GUI' should appear in your application menu shortly."
else
  echo "==> Can't write $LAUNCHER_DIR from this account."
  echo "    Run this once as your own user to add it to your menu:"
  echo
  echo "      cp '$STAGED_DESKTOP' '$LAUNCHER_DIR/' && \\"
  echo "        update-desktop-database '$LAUNCHER_DIR' 2>/dev/null; true"
fi

echo
echo "==> Done. Launch directly with: $APPHOST"
