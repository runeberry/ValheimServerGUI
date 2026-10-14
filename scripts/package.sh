#!/usr/bin/env bash
#
# package.sh — one-command, containerized packaging of ValheimServerGUI (v3.0 / Avalonia).
#
# Run with no arguments to produce the distributable artifacts in ./dist:
#   * ValheimServerGUI-<ver>-win-x64.exe        (single-file Windows executable)
#   * ValheimServerGUI-<ver>-linux-x64.tar.gz   (Linux publish + .desktop + icon)
#   * ValheimServerGUI-<ver>-x86_64.AppImage    (portable Linux app)
#
# The build runs entirely inside a cacheable Docker image (packaging/Dockerfile) — the host only needs
# Docker. The container runs as the current user and reuses the host NuGet cache, so nothing in the repo
# ends up root-owned and repeat runs are fast. Artifacts are FRAMEWORK-DEPENDENT (they need the .NET 10
# desktop runtime installed on the target) — including the AppImage, so that AppImage is NOT fully
# self-contained; pass --self-contained to bundle the runtime into every artifact instead.
#
# Flags (optional):
#   --self-contained   bundle the .NET runtime into every artifact (larger; no runtime prereq)
#   --rebuild-image    force a rebuild of the packaging image (after editing packaging/Dockerfile)

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
IMAGE="vsg-packaging:latest"
OUT="$REPO_ROOT/dist"

SELF_CONTAINED=false
REBUILD=0
for arg in "$@"; do
  case "$arg" in
    --self-contained) SELF_CONTAINED=true ;;
    --rebuild-image)  REBUILD=1 ;;
    *) echo "usage: $0 [--self-contained] [--rebuild-image]" >&2; exit 2 ;;
  esac
done

command -v docker >/dev/null 2>&1 || { echo "!! docker is required" >&2; exit 1; }

# 1. Build the packaging image if it's missing (or forced). This is the cacheable, reused part.
if [[ "$REBUILD" == 1 ]] || ! docker image inspect "$IMAGE" >/dev/null 2>&1; then
  echo ">> Building packaging image $IMAGE (one-time; cached afterwards)…"
  docker build -t "$IMAGE" -f "$REPO_ROOT/packaging/Dockerfile" "$REPO_ROOT/packaging" || exit 1
fi

mkdir -p "$OUT"

# 2. Run the packaging inside the container. Repo mounted read-only (all build output is redirected
#    out-of-tree by Directory.Build.props); NuGet cache reused from the host; artifacts written to ./dist.
NUGET_CACHE="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
mkdir -p "$NUGET_CACHE"

echo ">> Packaging in container (self-contained=$SELF_CONTAINED)…"
docker run --rm \
  -u "$(id -u):$(id -g)" \
  -e HOME=/tmp \
  -e NUGET_PACKAGES=/tmp/nuget \
  -e SELF_CONTAINED="$SELF_CONTAINED" \
  -e OUT_DIR=/out \
  -v "$REPO_ROOT:/src:ro" \
  -v "$OUT:/out" \
  -v "$NUGET_CACHE:/tmp/nuget" \
  "$IMAGE" bash /src/packaging/build-packages.sh /src
rc=$?

if [[ $rc -eq 0 ]]; then
  echo ""
  echo "ALL CLEAR — artifacts in $OUT"
else
  echo "!! packaging failed (exit $rc)" >&2
fi
exit $rc
