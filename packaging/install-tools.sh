#!/usr/bin/env bash
#
# install-tools.sh — installs the non-.NET tooling that build-packages.sh needs (appimagetool and its
# helpers) on a Debian-based system. Must run as root. Shared by the packaging image (packaging/Dockerfile)
# and the release workflow (.forgejo/workflows/release.yml) so both build with the same tools.
#
# FUSE isn't available inside containers, so appimagetool is unpacked (`--appimage-extract`, which needs no
# FUSE) and run from its AppRun via a /usr/local/bin/appimagetool shim.

set -euo pipefail

apt-get update
apt-get install -y --no-install-recommends \
    ca-certificates wget file zsync desktop-file-utils squashfs-tools
rm -rf /var/lib/apt/lists/*

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
wget -qO "$work/appimagetool.AppImage" \
    https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x "$work/appimagetool.AppImage"
( cd "$work" && ./appimagetool.AppImage --appimage-extract >/dev/null )
rm -rf /opt/appimagetool
mv "$work/squashfs-root" /opt/appimagetool
chmod -R a+rX /opt/appimagetool # extracted with 0700 dirs; package.sh builds as a non-root user
printf '#!/bin/sh\nexec /opt/appimagetool/AppRun "$@"\n' > /usr/local/bin/appimagetool
chmod +x /usr/local/bin/appimagetool
