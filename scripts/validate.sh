#!/usr/bin/env bash
#
# validate.sh — one-shot build + test + boot validation for ValheimServerGUI (v3.0 / Avalonia).
#
# Run with no arguments to validate the whole solution the way I (Mochi) always want it checked:
#   1. Build the solution  — MUST be 0 warnings, 0 errors.
#   2. Run every test       — Core + App + Serverless (fast: --no-build after step 1).
#   3. Headless boot smoke  — launch the Avalonia app under Xvfb; it must boot and STAY up
#                             with no unhandled exceptions (skipped automatically if Xvfb is absent).
#
# Output is deliberately terse: a per-phase [ OK ]/[FAIL]/[SKIP] plus the numbers I care about,
# and — only on failure — the actual error / failing-test lines inline so I don't have to re-run.
# Full logs for the run are kept in a temp dir whose path is printed at the end.
#
# Flags (all optional — the intent is "just run it"):
#   --quick   skip the Xvfb boot smoke (build + test only; good for pure logic changes)
#   --boot    boot smoke only (skip test) — rarely needed
#
# Keep this script in step with the workflow: if the solution file, project layout, or the way
# we validate changes, update this script (and the CLAUDE.local.md workflow note) in the same PR.

set -uo pipefail

# ---- repo layout (update here if it moves) --------------------------------------------------
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION="$REPO_ROOT/ValheimServerGUI.slnx"
APP_PROJECT="$REPO_ROOT/src/ValheimServerGUI.App/ValheimServerGUI.App.csproj"
BOOT_TIMEOUT=20   # seconds the app must survive under Xvfb to count as a healthy boot

RUN_BUILD=1; RUN_TEST=1; RUN_BOOT=1
case "${1:-}" in
  --quick) RUN_BOOT=0 ;;
  --boot)  RUN_BUILD=1; RUN_TEST=0 ;;
  "" ) ;;
  *) echo "usage: $0 [--quick|--boot]" >&2; exit 2 ;;
esac

LOGDIR="$(mktemp -d "${TMPDIR:-/tmp}/vsg-validate.XXXXXX")"
BUILD_LOG="$LOGDIR/build.log"
TEST_LOG="$LOGDIR/test.log"
BOOT_LOG="$LOGDIR/boot.log"

cleanup() {
  # Never leave a stray app/Xvfb behind if the boot smoke is interrupted.
  pkill -f "ValheimServerGUI.App" 2>/dev/null || true
  pkill -f "Xvfb" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

section() { printf '\n=== %s ===\n' "$1"; }
ok()   { printf '[ OK ] %s\n' "$1"; }
fail() { printf '[FAIL] %s\n' "$1"; }
skip() { printf '[SKIP] %s\n' "$1"; }

build_status="skipped"; test_status="skipped"; boot_status="skipped"
warn_count=0

# ---- 1. build -------------------------------------------------------------------------------
if [[ $RUN_BUILD -eq 1 ]]; then
  section "Build ($(basename "$SOLUTION"))"
  if dotnet build "$SOLUTION" >"$BUILD_LOG" 2>&1; then
    warn_count="$(grep -cE 'warning [A-Z]+[0-9]+' "$BUILD_LOG" || true)"
    if [[ "$warn_count" -gt 0 ]]; then
      fail "build succeeded but with $warn_count warning(s) — treated as failure (0-warning rule)"
      grep -E 'warning [A-Z]+[0-9]+' "$BUILD_LOG" | sort -u | head -30
      build_status="fail"
    else
      ok "build succeeded, 0 warnings"
      build_status="pass"
    fi
  else
    fail "build failed"
    grep -E 'error [A-Z]+[0-9]+|error :|Build FAILED' "$BUILD_LOG" | head -40
    build_status="fail"
  fi
fi

# ---- 2. test --------------------------------------------------------------------------------
# --no-build keeps this to a couple of seconds; the build above already compiled everything.
if [[ $RUN_TEST -eq 1 && "$build_status" != "fail" ]]; then
  section "Test"
  test_build_flag="--no-build"
  [[ $RUN_BUILD -eq 0 ]] && test_build_flag=""   # allow test-only invocation to build if needed
  if dotnet test "$SOLUTION" $test_build_flag >"$TEST_LOG" 2>&1; then
    grep -E 'Passed!|Failed!' "$TEST_LOG"
    ok "all tests passed"
    test_status="pass"
  else
    grep -E 'Passed!|Failed!' "$TEST_LOG"
    fail "one or more tests failed"
    # Surface the failing test names + assertion messages inline.
    grep -E '\[FAIL\]|Failed ' "$TEST_LOG" | head -40
    test_status="fail"
  fi
elif [[ $RUN_TEST -eq 1 ]]; then
  skip "test (build failed)"
fi

# ---- 3. headless boot smoke -----------------------------------------------------------------
if [[ $RUN_BOOT -eq 1 && "$build_status" != "fail" ]]; then
  section "Boot smoke (Xvfb, ${BOOT_TIMEOUT}s)"
  if ! command -v xvfb-run >/dev/null 2>&1; then
    skip "boot smoke (xvfb-run not installed)"
  else
    # timeout returns 124 when it kills a still-running process — for a GUI app that IS the
    # success signal (it booted and stayed up). Any other exit = it died early.
    timeout "$BOOT_TIMEOUT" xvfb-run -a dotnet run --project "$APP_PROJECT" --no-build >"$BOOT_LOG" 2>&1
    boot_code=$?
    cleanup
    if grep -iqE 'exception|unhandled|fatal error' "$BOOT_LOG"; then
      fail "boot smoke: app logged an error"
      grep -iE 'exception|unhandled|fatal error' "$BOOT_LOG" | head -20
      boot_status="fail"
    elif [[ $boot_code -eq 124 ]]; then
      ok "app booted and stayed up for ${BOOT_TIMEOUT}s (no errors)"
      boot_status="pass"
    else
      fail "boot smoke: app exited early (code $boot_code)"
      tail -20 "$BOOT_LOG"
      boot_status="fail"
    fi
  fi
elif [[ $RUN_BOOT -eq 1 ]]; then
  skip "boot smoke (build failed)"
fi

# ---- summary --------------------------------------------------------------------------------
section "Summary"
printf 'build : %s%s\n' "$build_status" "$([[ "$build_status" == pass ]] && echo " (0 warnings)" || true)"
printf 'test  : %s\n' "$test_status"
printf 'boot  : %s\n' "$boot_status"
printf 'logs  : %s\n' "$LOGDIR"

# Non-zero exit if any run phase failed, so callers/CI can gate on it.
if [[ "$build_status" == "fail" || "$test_status" == "fail" || "$boot_status" == "fail" ]]; then
  exit 1
fi
echo "ALL CLEAR"
