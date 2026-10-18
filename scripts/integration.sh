#!/usr/bin/env bash
#
# integration.sh — the COMPLETE test suite: the build + unit tests (scripts/validate.sh --quick) on the host,
# then the LIVE smoke against a real Valheim dedicated server in every server configuration, each in its own
# container, in parallel.
#
# Server configurations are the final stages of tests/integration-image/Dockerfile:
#   vanilla           - the current public dedicated server build
#   bepinex           - the same server with BepInExPack_Valheim and the plugins pinned in server-configs.env
#   bepinex-external  - the same pack + plugins kept outside the server folder, loaded via the configured paths
#
# Each container holds the .NET SDK and the server install, and runs `dotnet test -p:Integration=true` on the
# integration project against the read-only repo, so ValheimServer launches the real binary as a local child
# process exactly as the app does. Containers have their own network namespace, so the fixed test port never
# collides, and each builds into its own $HOME, so parallel builds never share output.
#
# What each configuration proves (all against the REAL binary, no game client):
#   - the server boots to Running off the real "Game server connected" line;
#   - with crossplay on, the parser raises an invite code once PlayFab registers the session;
#   - a graceful SIGINT stop flushes a world save, and a hard kill does NOT (proving the graceful path matters);
#   - the world save parses with the player-history reader;
#   - the live log still matches the ServerLogParser patterns (fixture-drift guard), captured as an artifact;
#   - mods load exactly when installed (BepInEx loads its plugins; vanilla shows no mod loader).
#
# Needs Docker, and outbound connectivity to Steam (image build downloads the server; reaching Running needs
# the Steam relay). The first run downloads ~2 GB and is cached as an image layer afterwards.
#
# Usage: integration.sh [--keep] [--update-server] [config...]
#   --keep            keep each configuration's savedir (in the printed run-log dir) for inspection
#   --update-server   re-download the dedicated server (picks up a new Valheim build)
#   config...         run only these configurations (default: all)
#
# Keep this script + tests/integration-image/Dockerfile in step with the test project as the workflow
# evolves (same rule as validate.sh).

set -uo pipefail

ALL_CONFIGS=(vanilla bepinex bepinex-external)

KEEP=0
SERVER_CACHE_BUST=0
CONFIGS=()
for arg in "$@"; do
  case "$arg" in
    --keep) KEEP=1 ;;
    --update-server) SERVER_CACHE_BUST="$(date +%s)" ;;
    vanilla|bepinex|bepinex-external) CONFIGS+=("$arg") ;;
    *) echo "usage: $0 [--keep] [--update-server] [${ALL_CONFIGS[*]}]..." >&2; exit 2 ;;
  esac
done
[[ ${#CONFIGS[@]} -eq 0 ]] && CONFIGS=("${ALL_CONFIGS[@]}")

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
IMAGE_DIR="$REPO_ROOT/tests/integration-image"
TEST_PROJECT="tests/ValheimServerGUI.Integration.Tests/ValheimServerGUI.Integration.Tests.csproj"
ARTIFACT_ROOT="$REPO_ROOT/dist/integration"
NUGET_CACHE="${NUGET_PACKAGES:-$HOME/.nuget/packages}"

# Hang guard (mirrors validate.sh), applied inside each container. The live tests self-bound every await with
# a finite timeout, so these mainly backstop a wedge in the test infra itself. Thresholds are generous: a cold
# first boot (world-gen + Steam connect) plus a graceful stop can legitimately take a few minutes.
TEST_HANG_TIMEOUT=360    # per-test seconds before --blame-hang aborts and dumps
TEST_HARD_TIMEOUT=1200   # wall-clock backstop if the hang guard itself wedges

section() { printf '\n=== %s ===\n' "$1"; }
ok()   { printf '[ OK ] %s\n' "$1"; }
fail() { printf '[FAIL] %s\n' "$1"; }

command -v docker >/dev/null 2>&1 || { fail "docker is required"; exit 1; }

RUNLOG="$(mktemp -d "${TMPDIR:-/tmp}/vsg-integration.XXXXXX")"
CONTAINER_PREFIX="vsg-it-$$"

cleanup() {
  # Removing the containers stops every server and testhost they hold; nothing outside them is touched.
  for config in "${CONFIGS[@]}"; do
    docker rm -f "$CONTAINER_PREFIX-$config" >/dev/null 2>&1 || true
  done
  if [[ $KEEP -eq 0 ]]; then
    rm -rf "$RUNLOG"/savedir-* 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

# ---- 1. build + unit tests (host) -----------------------------------------------------------
section "Build + unit tests (validate.sh --quick)"
if "$REPO_ROOT/scripts/validate.sh" --quick >"$RUNLOG/validate.log" 2>&1; then
  ok "build clean, unit tests passed"
else
  fail "validate.sh --quick failed; not starting the live smoke"
  tail -n 30 "$RUNLOG/validate.log"
  exit 1
fi

# ---- 2. images ------------------------------------------------------------------------------
section "Server images"
# Every KEY=value in server-configs.env becomes a build arg: that file is the single source of the mod pins.
BUILD_ARGS=(--build-arg "SERVER_CACHE_BUST=$SERVER_CACHE_BUST")
# shellcheck source=tests/integration-image/server-configs.env
source "$IMAGE_DIR/server-configs.env"
while IFS= read -r key; do
  BUILD_ARGS+=(--build-arg "$key=${!key}")
done < <(grep -oP '^[A-Z][A-Z0-9_]*(?==)' "$IMAGE_DIR/server-configs.env")
for config in "${CONFIGS[@]}"; do
  if docker build --target "$config" -t "vsg-integration:$config" "${BUILD_ARGS[@]}" \
      "$IMAGE_DIR" >"$RUNLOG/image-$config.log" 2>&1; then
    buildid="$(docker run --rm "vsg-integration:$config" \
      grep -E '"buildid"' /opt/steam/steamapps/appmanifest_896660.acf 2>/dev/null | tr -dc '0-9')"
    ok "$config (server build ${buildid:-unknown})"
  else
    fail "$config image build failed"
    tail -n 30 "$RUNLOG/image-$config.log"
    exit 1
  fi
done

# ---- 3. live smoke, one container per configuration, in parallel ----------------------------
section "Live smoke (${CONFIGS[*]})"
echo "(each boots a real server; first boot does world-gen + Steam connect — allow a couple of minutes)"
mkdir -p "$NUGET_CACHE"

declare -A PIDS
for config in "${CONFIGS[@]}"; do
  artifacts="$ARTIFACT_ROOT/$config"
  savedir="$RUNLOG/savedir-$config"
  # A stale blame sequence file from an earlier run would be misread as this run's hang.
  rm -rf "$artifacts/blame"
  mkdir -p "$artifacts" "$savedir"

  # The suite runs as the host user, who needs a passwd entry in the container: PlayFab Party (crossplay) looks the
  # user up at load time and crashes the server (SIGSEGV in its logger) when getpwuid() finds no one.
  passwd="$RUNLOG/passwd-$config"
  { docker run --rm "vsg-integration:$config" cat /etc/passwd
    echo "vsg:x:$(id -u):$(id -g):VSG integration:/tmp:/bin/sh"; } >"$passwd"

  # --blame-hang bounds + diagnoses a wedged test (dump + sequence file); `timeout` is the backstop.
  docker run --rm --init --name "$CONTAINER_PREFIX-$config" \
    -u "$(id -u):$(id -g)" \
    -v "$passwd:/etc/passwd:ro" \
    -e HOME=/tmp \
    -e NUGET_PACKAGES=/tmp/nuget \
    -e VSG_IT_SAVEDIR=/save \
    -e VSG_IT_ARTIFACT_DIR=/out \
    -v "$REPO_ROOT:/src:ro" \
    -v "$artifacts:/out" \
    -v "$savedir:/save" \
    -v "$NUGET_CACHE:/tmp/nuget" \
    "vsg-integration:$config" \
    timeout --kill-after=30 "$TEST_HARD_TIMEOUT" \
      dotnet test "/src/$TEST_PROJECT" -p:Integration=true \
        --blame-hang --blame-hang-timeout "${TEST_HANG_TIMEOUT}s" --blame-hang-dump-type mini \
        --results-directory /out/blame \
    >"$RUNLOG/test-$config.log" 2>&1 &
  PIDS[$config]=$!
done

# Classify one configuration's result from its exit code and log. A REAL hang leaves a blame sequence file
# naming a test that never finished (Completed="False"); a host abort with no such file is usually
# blame-hang's watchdog tripping on a load-slowed host. (Same classification as validate.sh.)
report() {
  local config="$1" code="$2" log="$RUNLOG/test-$1.log" blame="$ARTIFACT_ROOT/$1/blame" hangseq
  grep -E 'Passed!|Failed!|Skipped!' "$log" | sed "s/^/  $config: /"
  if [[ $code -eq 0 ]]; then
    ok "$config: all live tests passed"
    return 0
  fi
  if [[ $code -eq 124 || $code -eq 137 ]]; then
    fail "$config: hit the ${TEST_HARD_TIMEOUT}s hard timeout — the hang guard did not abort in time"
  elif grep -qE 'Failed:  *[1-9]' "$log"; then
    fail "$config: one or more tests failed"
    # Surface the failing tests + assertion messages (the line after "Error Message:") inline so the failure
    # is readable without re-running.
    grep -E -A1 '\[FAIL\]|Error Message' "$log" | grep -vE '^--$|Stack Trace' | head -40
  elif hangseq="$(grep -rlZ 'Completed="False"' "$blame" 2>/dev/null | tr '\0' '\n' | grep -iE 'sequence' | head -1)" \
      && [[ -n "$hangseq" ]]; then
    fail "$config: ABORTED ON A HANG — a test never completed (>${TEST_HANG_TIMEOUT}s)"
    echo "  stuck test: $(grep 'Completed="False"' "$hangseq" | grep -oiE 'name="[^"]*"' | head -1)"
    echo "  hang artifacts (sequence + dump): $blame"
  elif grep -qiE 'Test host process crashed|Sequence file will not be generated|inactivity time' "$log"; then
    fail "$config: test host aborted with NO stuck test — likely blame-hang's watchdog on a load-slowed host. Re-run."
  else
    fail "$config: tests failed (exit $code)"
    grep -E '\[FAIL\]|Failed |Error Message' "$log" | head -40
  fi
  echo "  (full output: $log)"
  return 1
}

FAILED=()
for config in "${CONFIGS[@]}"; do
  wait "${PIDS[$config]}"
  report "$config" $? || FAILED+=("$config")
done

# ---- summary --------------------------------------------------------------------------------
section "Summary"
for config in "${CONFIGS[@]}"; do
  if [[ " ${FAILED[*]} " == *" $config "* ]]; then status="fail"; else status="pass"; fi
  printf '%-16s : %s  (artifacts: %s)\n' "$config" "$status" "$ARTIFACT_ROOT/$config"
done
printf 'run log  : %s\n' "$RUNLOG"
printf 'savedirs : %s\n' "$([[ $KEEP -eq 1 ]] && echo "kept in $RUNLOG" || echo "removed")"

if [[ ${#FAILED[@]} -gt 0 ]]; then
  exit 1
fi
echo "ALL CLEAR"
