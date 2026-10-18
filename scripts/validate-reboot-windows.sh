#!/usr/bin/env bash
#
# validate-reboot-windows.sh — proves "Start with the operating system" on Windows the way a user meets it: set the
# checkbox through VSG's own UI, reboot, and check whether Windows launched the installed exe at login. Runs in a
# disposable Windows 11 VM; on demand only (about 10 minutes warm).
#
# Image: the integration OS layer (scripts/lib/windows-vm.sh) plus a desktop layer (tests/reboot-vm/bake-desktop.ps1):
# the official .NET Desktop Runtime with no SDK on the path, the vsg-ui driver task, and a canary startup entry.
# The desktop layer is rebuilt with the OS layer, on --rebuild-image, or when its inputs change.
#
# Each reboot is judged against the canary, a second Run value in the same login startup pass as VSG's (see
# scripts/lib/reboot-steps.sh).
#
# Sequence (each step drives the UI, then reboots):
#   1. enable                                        => launched at login
#   2. disable                                       => not launched
#   3. enable, then a Settings > Startup disable     => not launched   (StartupApproved\Run, as Windows writes it)
#   4. expect-unchecked-then-enable                  => launched       (the checkbox must read the Settings disable)
#
# Usage: validate-reboot-windows.sh [--keep] [--rebuild-image] [--artifact <exe>]
#   --artifact <exe>   test this exe instead of building one with scripts/package.sh
#   --rebuild-image    rebuild the OS layer (and the desktop layer on top of it)
#   --keep             leave the VM up afterwards
#
# Keep this script + tests/reboot-vm/* in step with the app's start-with-OS behavior (same rule as validate.sh).

set -uo pipefail

KEEP=0
REBUILD_IMAGE=0
ARTIFACT=""
usage() { echo "usage: $0 [--keep] [--rebuild-image] [--artifact <exe>]" >&2; exit 2; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --keep) KEEP=1 ;;
    --rebuild-image) REBUILD_IMAGE=1 ;;
    --artifact) [[ -n "${2:-}" ]] || usage; ARTIFACT="$(realpath "$2")"; shift ;;
    *) usage ;;
  esac
  shift
done

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/libvirt-vm.sh
source "$REPO_ROOT/scripts/lib/libvirt-vm.sh"
# shellcheck source=scripts/lib/windows-vm.sh
source "$REPO_ROOT/scripts/lib/windows-vm.sh"
# shellcheck source=scripts/lib/reboot-steps.sh
source "$REPO_ROOT/scripts/lib/reboot-steps.sh"

UI_DIR="$REPO_ROOT/tests/reboot-vm"
DESKTOP_VOL=vsg-win11-desktop.qcow2
DESKTOP_INPUTS_HASH="$(cat "$UI_DIR/windows.env" "$UI_DIR/bake-desktop.ps1" | sha256sum | cut -c1-16)"
ARTIFACT_ROOT="$REPO_ROOT/dist/reboot-windows"
DOM="vsg-reboot-win-$$"
# A spaced install folder, like a user's, so an unquoted Run command would break.
INSTALL_DIR='C:\Users\vsg\My Apps\ValheimServerGUI'
INSTALL_EXE="$INSTALL_DIR\\ValheimServerGUI.exe"
RUN_VALUE=ValheimServerGUI   # AppConstants.StartupKey

DRIVE_TIMEOUT=180    # one UI action, including launching the app
# shellcheck disable=SC2034  # read by scripts/lib/reboot-steps.sh
STEP_DIR="$ARTIFACT_ROOT"

# ---- image -----------------------------------------------------------------------------------

build_desktop_layer() {
  local dom=vsg-win-build-desktop ip t0=$SECONDS
  destroy_vm "$dom"
  vol_delete "$DESKTOP_VOL"
  rm -f "$CACHE/desktop.stamp"

  create_overlay "$DESKTOP_VOL" "$OS_VOL" || return 1
  define_vm "$dom" --disk "vol=$POOL/$DESKTOP_VOL,bus=sata" || return 1
  ip="$(wait_ssh "$dom" "$BOOT_TIMEOUT")" || { fail "the desktop build VM did not reach SSH"; return 1; }

  # shellcheck source=tests/reboot-vm/windows.env
  source "$UI_DIR/windows.env"
  vm_scp "$UI_DIR/bake-desktop.ps1" "$VM_USER@$ip:C:/bake-desktop.ps1" || return 1
  vm_ssh "$ip" "powershell -NoProfile -ExecutionPolicy Bypass -File C:\\bake-desktop.ps1 \
    -RuntimeUrl \"$RUNTIME_URL\" -RuntimeSha512 \"$RUNTIME_SHA512\"" >"$RUNLOG/bake-desktop.log" 2>&1 \
    || { fail "bake-desktop.ps1"; tail -n 30 "$RUNLOG/bake-desktop.log"; return 1; }
  vm_ssh "$ip" "del C:\\bake-desktop.ps1" >/dev/null

  shutdown_vm "$dom" "$ip" "$WIN_POWEROFF" || { fail "the desktop build VM did not shut down"; return 1; }
  "${VIRSH[@]}" undefine --nvram "$dom" >/dev/null
  echo "$(date +%s) - $DESKTOP_INPUTS_HASH" > "$CACHE/desktop.stamp"
  ok "desktop layer built in $(( (SECONDS - t0) / 60 )) min"
}

# ---- guest hooks for scripts/lib/reboot-steps.sh ---------------------------------------------

guest_reboot() { reboot_vm "$DOM" "$IP"; }

guest_status() { vm_ssh "$IP" "powershell -NoProfile -ExecutionPolicy Bypass -File C:\\vsg\\ui\\status.ps1" 2>/dev/null | tr -d '\r'; }

# Starts the action in the interactive session (the vsg-ui task), waits for its result, prints log then result.
guest_drive() { # action
  local result="" t0=$SECONDS
  vm_ps "$IP" "Remove-Item C:\\vsg\\out\\$1.result -ErrorAction SilentlyContinue; Set-Content C:\\vsg\\ui\\args.txt '$1'" >/dev/null
  vm_ssh "$IP" "schtasks /run /tn vsg-ui" >/dev/null || { echo "FAIL: could not start the UI driver task"; return 1; }
  while ((SECONDS - t0 < DRIVE_TIMEOUT)); do
    sleep 5
    result="$(vm_ps "$IP" "Get-Content C:\\vsg\\out\\$1.result -ErrorAction SilentlyContinue" 2>/dev/null | tr -d '\r')"
    [[ -n "$result" ]] && break
  done
  vm_ps "$IP" "Get-Content C:\\vsg\\out\\$1.log -ErrorAction SilentlyContinue" 2>/dev/null
  echo "${result:-FAIL: no result after ${DRIVE_TIMEOUT}s}"
}

# What Settings > Apps > Startup writes when the user switches the entry off: a 01 flag, three zero bytes, and the
# FILETIME of the change.
settings_disable() {
  if ! vm_ps "$IP" "New-Item -Force 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run' | Out-Null; \
    Set-ItemProperty 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run' $RUN_VALUE \
    ([byte[]](1,0,0,0) + [BitConverter]::GetBytes((Get-Date).ToFileTimeUtc())) -Type Binary" >/dev/null; then
    fail "step $STEP: could not write the StartupApproved value"
    return 1
  fi
  ok "step $STEP: switched VSG off as Settings > Startup does"
}

FAILED=0
IP=""

run_sequence() {
  step "enable => launched at login"
  drive enable && reboot_and_expect launched || return 1
  step "disable => not launched"
  drive disable && reboot_and_expect not-launched || return 1
  step "enable, then disable in Settings > Startup => not launched"
  drive enable && settings_disable && reboot_and_expect not-launched || return 1
  step "checkbox reads the Settings disable; enable => launched"
  drive expect-unchecked-then-enable && reboot_and_expect launched || return 1
}

# ---- main ------------------------------------------------------------------------------------

vm_preflight xorriso
RUNLOG="$(mktemp -d "${TMPDIR:-/tmp}/vsg-reboot-windows.XXXXXX")"
rm -rf "$ARTIFACT_ROOT"; mkdir -p "$ARTIFACT_ROOT"

cleanup() {
  [[ $KEEP -eq 1 ]] && return
  destroy_vm "$DOM" "$DOM.qcow2"
}
trap cleanup EXIT INT TERM

section "Artifact"
if [[ -z "$ARTIFACT" ]]; then
  version="$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/src/ValheimServerGUI.Core/ValheimServerGUI.Core.csproj")"
  log "building packages (scripts/package.sh)"
  "$REPO_ROOT/scripts/package.sh" >"$RUNLOG/package.log" 2>&1 || { tail -n 30 "$RUNLOG/package.log"; die "package.sh failed"; }
  # By version, never by glob: dist/ keeps older builds.
  ARTIFACT="$REPO_ROOT/dist/ValheimServerGUI-$version-win-x64.exe"
fi
[[ -f "$ARTIFACT" ]] || die "no artifact at $ARTIFACT"
ok "testing $ARTIFACT"

section "Windows image"
ensure_os_image "$REBUILD_IMAGE"
if child_layer_stale "$DESKTOP_VOL" "$CACHE/desktop.stamp" "$DESKTOP_INPUTS_HASH"; then
  log "building the desktop layer"
  build_desktop_layer || die "desktop layer build failed (VM vsg-win-build-desktop left up for inspection; the next build removes it)"
fi
ok "OS layer $OS_AGE_DAYS days old (evaluation expires in $((90 - OS_AGE_DAYS)) days)"

section "VM"
create_overlay "$DOM.qcow2" "$DESKTOP_VOL" || die "could not create the VM disk"
define_vm "$DOM" --disk "vol=$POOL/$DOM.qcow2,bus=sata" || die "could not create the VM"
IP="$(wait_ssh "$DOM" "$BOOT_TIMEOUT")" || die "the VM did not reach SSH"
wait_interactive_session "$IP" >/dev/null || die "no interactive session for $VM_USER"
vm_ps "$IP" "New-Item -ItemType Directory -Force '$INSTALL_DIR', C:\\vsg\\ui, C:\\vsg\\out | Out-Null; Set-Content C:\\vsg\\ui\\exe.txt '$INSTALL_EXE'" >/dev/null \
  || die "could not prepare the VM"
# Windows' scp takes the remote path literally (SFTP), spaces included.
vm_scp "$ARTIFACT" "$VM_USER@$IP:${INSTALL_EXE//\\//}" || die "could not install the app"
vm_scp "$UI_DIR/drive.ps1" "$UI_DIR/status.ps1" "$VM_USER@$IP:C:/vsg/ui/" || die "could not copy the UI driver"
ok "VM $DOM up at $IP, app installed at $INSTALL_EXE"

run_sequence || FAILED=1

section "Summary"
printf 'artifact  : %s\n' "$ARTIFACT"
printf 'results   : %s (driver logs, screenshots)\n' "$ARTIFACT_ROOT"
printf 'run log   : %s\n' "$RUNLOG"
if [[ $KEEP -eq 1 ]]; then
  printf 'kept VM   : %s at %s (ssh -i %s %s@%s; remove: virsh -c %s destroy %s; virsh -c %s undefine --nvram %s; virsh -c %s vol-delete --pool %s %s.qcow2)\n' \
    "$DOM" "$IP" "$KEY" "$VM_USER" "$IP" "$LIBVIRT_URI" "$DOM" "$LIBVIRT_URI" "$DOM" "$LIBVIRT_URI" "$POOL" "$DOM"
fi
[[ $FAILED -eq 0 ]] || exit 1
echo "ALL CLEAR"
