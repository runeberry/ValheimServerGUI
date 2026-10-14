#!/usr/bin/env bash
# Build the Release artifacts from the tagged source (the current directory): fill the client
# secrets, run the test suite, then package the Windows exe, Linux tarball, and AppImage into
# OUT_DIR with the same script scripts/package.sh runs in its container. Like the other ci/
# scripts, the packaging script comes from the workflow ref, so fixes apply to re-runs of a tag.
#
# Env: RUNEBERRY_API_KEY_HEADER, RUNEBERRY_CLIENT_API_KEY, OUT_DIR.
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }

[ -n "${RUNEBERRY_API_KEY_HEADER:-}" ] || die "RUNEBERRY_API_KEY_HEADER secret is not set."
[ -n "${RUNEBERRY_CLIENT_API_KEY:-}" ] || die "RUNEBERRY_CLIENT_API_KEY secret is not set."
[ -n "${OUT_DIR:-}" ] || die "OUT_DIR is not set."

# The template holds C# verbatim strings (@"..."), where a quote is escaped by doubling it.
shopt -u patsub_replacement 2>/dev/null || true
header="${RUNEBERRY_API_KEY_HEADER//\"/\"\"}"
key="${RUNEBERRY_CLIENT_API_KEY//\"/\"\"}"
template="$(cat SolutionResources/ClientSecrets.Values.cs.template)"
template="${template//__RUNEBERRY_API_KEY_HEADER__/$header}"
template="${template//__RUNEBERRY_CLIENT_API_KEY__/$key}"
printf '%s\n' "$template" > SolutionResources/ClientSecrets.Values.cs

# Fail before building if the API refuses the key: a stale secret otherwise ships a build whose
# update checks and crash reports are all rejected. The URL comes from the tagged source.
api_url="$(grep -oP 'UrlRuneberryApi\s*=\s*"\K[^"]+' src/ValheimServerGUI.Core/CoreConstants.cs)" \
  || die "could not find UrlRuneberryApi in CoreConstants.cs."
status="$(curl -s -o /dev/null -w '%{http_code}' --retry 3 \
  -H "${RUNEBERRY_API_KEY_HEADER}: ${RUNEBERRY_CLIENT_API_KEY}" "${api_url}/update-check")" || true
[ "$status" = 200 ] || die "the Runeberry API answered ${status} to ${api_url}/update-check with the client key secrets (expected 200)."

# Same hang guard as scripts/validate.sh: a deadlocked test fails the build instead of stalling it.
dotnet test ValheimServerGUI.slnx -c Release --blame-hang --blame-hang-timeout 120s

bash "$(dirname "$0")/../packaging/build-packages.sh" "$PWD"

ls "$OUT_DIR"/*-win-x64.exe >/dev/null 2>&1 || die "packaging did not produce the Windows exe."
