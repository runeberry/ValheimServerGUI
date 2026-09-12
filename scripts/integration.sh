#!/usr/bin/env bash
#
# integration.sh — tier-4 LIVE smoke against a real Valheim dedicated server (docs/V3_TARGET_STATE.md
# §13, matrix E9). Mirrors scripts/validate.sh in shape: a terse per-phase [ OK ]/[FAIL]/[SKIP], a temp
# run-log dir printed at the end, and a non-zero exit if anything failed.
#
# What it proves (all against the REAL binary, no game client):
#   - the server boots to Running off the real "Game server connected" line;
#   - a graceful SIGINT stop flushes a world save, and a hard kill does NOT (the E9 A/B);
#   - a Steam Cloud world imports into a local savedir and lists;
#   - the live log still matches the ServerLogParser patterns (fixture-drift guard), captured as an artifact.
#
# Machine-specific Steam paths come from scripts/integration.local.env (gitignored). If that file is
# absent this script SKIPS (exit 0) with instructions — it never fails just because the box isn't set up.
#
# Network dependency: reaching Running needs outbound connectivity to Steam. If Steam is unreachable the
# boot test fails loudly within the timeout (it does not hang silently).
#
# Flag:
#   --keep   keep the temp savedir and run-log dir for inspection (otherwise removed on exit)
#
# Keep this script + scripts/integration.local.env.example + the CLAUDE.local.md note in step with the
# test project as the workflow evolves (same rule as validate.sh).

set -uo pipefail

KEEP=0
case "${1:-}" in
  --keep) KEEP=1 ;;
  "" ) ;;
  *) echo "usage: $0 [--keep]" >&2; exit 2 ;;
esac

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$REPO_ROOT/scripts/integration.local.env"
ENV_EXAMPLE="$REPO_ROOT/scripts/integration.local.env.example"
TEST_PROJECT="$REPO_ROOT/tests/ValheimServerGUI.Integration.Tests"
ARTIFACT_DIR="$REPO_ROOT/dist/integration"

# Hang guard (mirrors validate.sh). The live tests self-bound every await with a finite timeout, so
# these mainly backstop a wedge in the test infra itself. Thresholds are generous: a cold first boot
# (world-gen + Steam connect) plus a graceful stop can legitimately take a few minutes.
TEST_HANG_TIMEOUT=360    # per-test seconds before --blame-hang aborts and dumps
TEST_HARD_TIMEOUT=1200   # outer wall-clock backstop if the hang guard itself wedges

section() { printf '\n=== %s ===\n' "$1"; }
ok()   { printf '[ OK ] %s\n' "$1"; }
fail() { printf '[FAIL] %s\n' "$1"; }
skip() { printf '[SKIP] %s\n' "$1"; }

# ---- 0. config ------------------------------------------------------------------------------
section "Config"
if [[ ! -f "$ENV_FILE" ]]; then
  skip "no scripts/integration.local.env — this machine isn't set up for the live smoke"
  echo
  echo "  To enable it, create the config from the template and fill in this box's Steam paths:"
  echo "    cp $ENV_EXAMPLE $ENV_FILE"
  echo "    \$EDITOR $ENV_FILE"
  echo
  echo "Nothing run (not a failure)."
  exit 0
fi

# set -a so every var the file defines is exported to the dotnet test child process (the env file is
# plain KEY=value, no `export` clutter); the test reads them via Environment.GetEnvironmentVariable.
set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a
ok "sourced $(basename "$ENV_FILE")"

# ---- 1. validate the server binary ----------------------------------------------------------
section "Server binary"
if [[ -z "${VSG_IT_SERVER_EXE:-}" ]]; then
  fail "VSG_IT_SERVER_EXE is not set in $ENV_FILE"
  exit 1
fi
if [[ ! -x "$VSG_IT_SERVER_EXE" ]]; then
  fail "server binary not found or not executable: $VSG_IT_SERVER_EXE"
  exit 1
fi
SERVER_DIR="$(dirname "$VSG_IT_SERVER_EXE")"
ok "found $(basename "$VSG_IT_SERVER_EXE")"

# ---- 2. isolated savedir + artifact/log dirs ------------------------------------------------
RUNLOG="$(mktemp -d "${TMPDIR:-/tmp}/vsg-integration.XXXXXX")"
SAVEDIR="$(mktemp -d "${TMPDIR:-/tmp}/vsg-it-savedir.XXXXXX")"
TEST_LOG="$RUNLOG/test.log"
mkdir -p "$ARTIFACT_DIR"

cleanup() {
  # Never leave a live server or orphaned testhost behind, even if a test bailed mid-boot or the run
  # was force-killed by the hang guard. (Patterns are specific enough not to match this script.)
  pkill -f "valheim_server" 2>/dev/null || true
  pkill -f "valheim-server-gui/artifacts/bin/ValheimServerGUI.Integration.Tests" 2>/dev/null || true
  if [[ $KEEP -eq 0 ]]; then
    rm -rf "$SAVEDIR" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

# Point every live test at the mochi-owned savedir + the persistent artifact dir.
export VSG_IT_SAVEDIR="$SAVEDIR"
export VSG_IT_ARTIFACT_DIR="$ARTIFACT_DIR"
# Belt-and-suspenders: the Core launch fix already sets these on the child process, but exporting them
# here too means even an older Core build would resolve steamclient.so for the live server.
export SteamAppId=892970
export LD_LIBRARY_PATH="${SERVER_DIR}:${SERVER_DIR}/linux64${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"

# ---- 3. run the live integration tests ------------------------------------------------------
section "Live integration tests"
echo "savedir : $SAVEDIR"
echo "artifacts: $ARTIFACT_DIR"
echo "(booting a real server; first boot does world-gen + Steam connect — allow a couple of minutes)"
echo

test_status="fail"
BLAME_DIR="$RUNLOG/blame"

# --blame-hang bounds + diagnoses a wedged test (dump + sequence file); outer `timeout` is the backstop.
timeout --kill-after=30 "$TEST_HARD_TIMEOUT" \
  dotnet test "$TEST_PROJECT" \
    --blame-hang --blame-hang-timeout "${TEST_HANG_TIMEOUT}s" --blame-hang-dump-type mini \
    --results-directory "$BLAME_DIR" \
    >"$TEST_LOG" 2>&1
test_code=$?

grep -E 'Passed!|Failed!|Skipped!' "$TEST_LOG"
if [[ $test_code -eq 0 ]]; then
  ok "all live integration tests passed"
  test_status="pass"
elif [[ $test_code -eq 124 || $test_code -eq 137 ]]; then
  fail "live tests hit the ${TEST_HARD_TIMEOUT}s hard timeout — the hang guard did not abort in time"
  echo "  (full output: $TEST_LOG)"
else
  # A REAL hang leaves a blame sequence file naming a test that never finished (Completed="False"). A
  # host abort with no such file is usually blame-hang's watchdog tripping on a load-slowed host, not a
  # real hang. (Same classification as validate.sh.)
  hangseq="$(grep -rlZ 'Completed="False"' "$BLAME_DIR" 2>/dev/null | tr '\0' '\n' | grep -iE 'sequence' | head -1)"
  if grep -qE 'Failed:  *[1-9]' "$TEST_LOG"; then
    fail "one or more live integration tests failed"
    # Surface the failing tests + assertion messages inline so the failure is readable without re-running.
    grep -E '\[FAIL\]|Failed |Error Message|Assert\.' "$TEST_LOG" | head -40
  elif [[ -n "$hangseq" ]]; then
    fail "live tests ABORTED ON A HANG — a test never completed (>${TEST_HANG_TIMEOUT}s)"
    echo "  stuck test: $(grep 'Completed="False"' "$hangseq" | grep -oiE 'name="[^"]*"' | head -1)"
    echo "  hang artifacts (sequence + dump): $BLAME_DIR"
  elif grep -qiE 'Test host process crashed|Sequence file will not be generated|inactivity time' "$TEST_LOG"; then
    fail "test host aborted with NO stuck test — likely blame-hang's watchdog on a load-slowed host, not a real hang. Re-run."
  else
    fail "live integration tests failed (exit $test_code)"
    grep -E '\[FAIL\]|Failed |Error Message|Skipped ' "$TEST_LOG" | head -40
  fi
  echo "  (full output: $TEST_LOG)"
fi

# ---- summary --------------------------------------------------------------------------------
section "Summary"
printf 'tests    : %s\n' "$test_status"
printf 'savedir  : %s%s\n' "$SAVEDIR" "$([[ $KEEP -eq 1 ]] && echo " (kept)" || echo " (removed)")"
printf 'artifacts: %s\n' "$ARTIFACT_DIR"
printf 'run log  : %s\n' "$RUNLOG"

if [[ "$test_status" != "pass" ]]; then
  exit 1
fi
echo "ALL CLEAR"
