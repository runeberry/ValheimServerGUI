#!/usr/bin/env bash
#
# gen-test-strings.sh — regenerate the key-echo test culture (Strings.qps-ploc.resx) from the
# English Strings.resx. Run it after adding, renaming, or removing any key, or after changing the
# placeholders in an English value. LocalizationTests fails if the two files drift apart.
#
# Usage: ./scripts/gen-test-strings.sh   (no arguments)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$REPO_ROOT"
dotnet run --file scripts/gen-test-strings.cs -- src/ValheimServerGUI.Localization
