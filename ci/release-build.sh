#!/usr/bin/env bash
# Build the Release configuration: fill the client secrets, compile the tests (they need
# Windows to run), and publish the single-file win-x64 exe to publish/small-x64/.
#
# Env: RUNEBERRY_API_KEY_HEADER, RUNEBERRY_CLIENT_API_KEY.
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }

[ -n "${RUNEBERRY_API_KEY_HEADER:-}" ] || die "RUNEBERRY_API_KEY_HEADER secret is not set."
[ -n "${RUNEBERRY_CLIENT_API_KEY:-}" ] || die "RUNEBERRY_CLIENT_API_KEY secret is not set."

# The template holds C# verbatim strings (@"..."), where a quote is escaped by doubling it.
shopt -u patsub_replacement 2>/dev/null || true
header="${RUNEBERRY_API_KEY_HEADER//\"/\"\"}"
key="${RUNEBERRY_CLIENT_API_KEY//\"/\"\"}"
template="$(cat SolutionResources/ClientSecrets.Values.cs.template)"
template="${template//__RUNEBERRY_API_KEY_HEADER__/$header}"
template="${template//__RUNEBERRY_CLIENT_API_KEY__/$key}"
printf '%s\n' "$template" > SolutionResources/ClientSecrets.Values.cs

dotnet build ValheimServerGUI.Tests/ValheimServerGUI.Tests.csproj -c Release
dotnet publish ValheimServerGUI/ValheimServerGUI.csproj -c Release -f net6.0-windows \
  -p:PublishProfile=small-x64-release -p:PublishDir="${PWD}/publish/small-x64/"

[ -f publish/small-x64/ValheimServerGUI.exe ] || die "publish did not produce ValheimServerGUI.exe."
