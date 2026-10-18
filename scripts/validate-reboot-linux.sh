#!/usr/bin/env bash
#
# validate-reboot-linux.sh — proves "Start with the operating system" on Linux the way a user meets it: set the
# checkbox through VSG's own UI, reboot, and check whether the desktop launched the installed app at login. Runs in
# disposable Ubuntu 24.04 VMs (GNOME on Wayland, the default session), one per artifact, in parallel; on demand only.
#
# Image: Ubuntu's cloud image plus a desktop layer configured by cloud-init (scripts/lib/linux-vm.sh,
# tests/reboot-vm/user-data.yaml): auto-logon, the .NET runtime from the Ubuntu archive, AT-SPI bindings, and a
# canary autostart entry the reboots are judged against (scripts/lib/reboot-steps.sh). Rebuilt on --rebuild-image
# or when its inputs change.
#
# Artifacts: the AppImage (installed as ~/My Apps/ValheimServerGUI.AppImage) and the tarball (extracted to
# ~/My Apps/ValheimServerGUI/). The spaced folder makes an unquoted Exec line fail.
#
# Sequence (each step drives the UI with tests/reboot-vm/drive.py, then reboots):
#   1. enable                                         => launched at login
#   2. disable                                        => not launched
#   3. enable, then Hidden=true added to the entry    => not launched   (an edit made outside the app)
#   4. expect-unchecked-then-enable                   => launched       (the checkbox must read the edit)
#
# Usage: validate-reboot-linux.sh [--keep] [--rebuild-image] [--artifact-appimage <file>] [--artifact-tarball <file>]
#                                 [appimage|tarball]...
#   --artifact-*       test this file instead of building one with scripts/package.sh
#   --rebuild-image    rebuild the desktop layer
#   --keep             leave the VMs up afterwards
#   appimage|tarball   run only these artifacts (default: both)
#
# Keep this script + tests/reboot-vm/* in step with the app's start-with-OS behavior (same rule as validate.sh).

set -uo pipefail

ALL_KINDS=(appimage tarball)
KEEP=0
REBUILD_IMAGE=0
declare -A ARTIFACT=()
KINDS=()
usage() { echo "usage: $0 [--keep] [--rebuild-image] [--artifact-appimage <file>] [--artifact-tarball <file>] [${ALL_KINDS[*]}]..." >&2; exit 2; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --keep) KEEP=1 ;;
    --rebuild-image) REBUILD_IMAGE=1 ;;
    --artifact-appimage) [[ -n "${2:-}" ]] || usage; ARTIFACT[appimage]="$(realpath "$2")"; shift ;;
    --artifact-tarball) [[ -n "${2:-}" ]] || usage; ARTIFACT[tarball]="$(realpath "$2")"; shift ;;
    appimage|tarball) KINDS+=("$1") ;;
    *) usage ;;
  esac
  shift
done
[[ ${#KINDS[@]} -eq 0 ]] && KINDS=("${ALL_KINDS[@]}")

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/libvirt-vm.sh
source "$REPO_ROOT/scripts/lib/libvirt-vm.sh"
# shellcheck source=scripts/lib/linux-vm.sh
source "$REPO_ROOT/scripts/lib/linux-vm.sh"
# shellcheck source=scripts/lib/reboot-steps.sh
source "$REPO_ROOT/scripts/lib/reboot-steps.sh"

UI_DIR="$REPO_ROOT/tests/reboot-vm"
DESKTOP_INPUTS_HASH="$(cat "$UI_DIR/linux.env" "$UI_DIR/user-data.yaml" | sha256sum | cut -c1-16)"
ARTIFACT_ROOT="$REPO_ROOT/dist/reboot-linux"
APPS_DIR='/home/vsg/My Apps'
declare -A TARGET=(
  [appimage]="$APPS_DIR/ValheimServerGUI.AppImage"
  [tarball]="$APPS_DIR/ValheimServerGUI/ValheimServerGUI.App"
)
ENTRY=/home/vsg/.config/autostart/ValheimServerGUI.desktop

# ---- guest hooks for scripts/lib/reboot-steps.sh ---------------------------------------------
# Set per artifact (each runs in its own background subshell): DOM, IP, KIND, STEP_DIR.

guest_reboot() { reboot_linux_vm "$DOM" "$IP"; }
guest_status() { vm_session "$IP" "python3 ~/drive.py status '${TARGET[$KIND]}'" 2>/dev/null; }
guest_drive()  { vm_session "$IP" "python3 ~/drive.py $1 '${TARGET[$KIND]}'"; }

hide_entry() {
  if ! vm_ssh "$IP" "echo Hidden=true >> $ENTRY"; then
    fail "step $STEP: could not edit the autostart entry"
    return 1
  fi
  ok "step $STEP: added Hidden=true to the autostart entry, as a user or tool editing it would"
}

install_artifact() {
  case "$KIND" in
    appimage)
      vm_ssh "$IP" "mkdir -p '$APPS_DIR' && cat > '${TARGET[appimage]}' && chmod +x '${TARGET[appimage]}'" <"${ARTIFACT[appimage]}" ;;
    tarball)
      vm_ssh "$IP" "mkdir -p '$APPS_DIR/ValheimServerGUI' && tar xzf - -C '$APPS_DIR/ValheimServerGUI' --strip-components=1" \
        <"${ARTIFACT[tarball]}" ;;
  esac
}

run_sequence() {
  step "enable => launched at login"
  drive enable && reboot_and_expect launched || return 1
  step "disable => not launched"
  drive disable && reboot_and_expect not-launched || return 1
  step "enable, then hide the autostart entry => not launched"
  drive enable && hide_entry && reboot_and_expect not-launched || return 1
  step "checkbox reads the hidden entry; enable => launched"
  drive expect-unchecked-then-enable && reboot_and_expect launched || return 1
}

# One artifact's whole run in its own VM; returns non-zero on any failure.
run_kind() { # kind
  KIND="$1"
  DOM="vsg-reboot-linux-$KIND-$$"
  STEP_DIR="$ARTIFACT_ROOT/$KIND"
  mkdir -p "$STEP_DIR"
  create_overlay "$DOM.qcow2" "$LINUX_DESKTOP_VOL" || { fail "could not create the VM disk"; return 1; }
  define_linux_vm "$DOM" --disk "vol=$POOL/$DOM.qcow2,bus=virtio" || { fail "could not create the VM"; return 1; }
  IP="$(wait_ssh "$DOM" "$LINUX_BOOT_TIMEOUT")" || { fail "the VM did not reach SSH"; return 1; }
  echo "$IP" >"$RUNLOG/ip-$KIND"
  wait_graphical_session "$IP" || { shot no-login; fail "no automatic GNOME login"; return 1; }
  install_artifact || { fail "could not install the $KIND"; return 1; }
  vm_scp "$UI_DIR/drive.py" "$VM_USER@$IP:drive.py" || { fail "could not copy the UI driver"; return 1; }
  ok "VM $DOM up at $IP, app installed at ${TARGET[$KIND]}"
  run_sequence
}

# ---- main ------------------------------------------------------------------------------------

vm_preflight virt-install
RUNLOG="$(mktemp -d "${TMPDIR:-/tmp}/vsg-reboot-linux.XXXXXX")"
rm -rf "$ARTIFACT_ROOT"; mkdir -p "$ARTIFACT_ROOT"

cleanup() {
  [[ $KEEP -eq 1 ]] && return
  for kind in "${KINDS[@]}"; do destroy_vm "vsg-reboot-linux-$kind-$$" "vsg-reboot-linux-$kind-$$.qcow2"; done
}
trap cleanup EXIT INT TERM

section "Artifacts"
version="$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/src/ValheimServerGUI.Core/ValheimServerGUI.Core.csproj")"
need_package=0
for kind in "${KINDS[@]}"; do [[ -z "${ARTIFACT[$kind]:-}" ]] && need_package=1; done
if [[ $need_package -eq 1 ]]; then
  log "building packages (scripts/package.sh)"
  "$REPO_ROOT/scripts/package.sh" >"$RUNLOG/package.log" 2>&1 || { tail -n 30 "$RUNLOG/package.log"; die "package.sh failed"; }
fi
# By version, never by glob: dist/ keeps older builds.
: "${ARTIFACT[appimage]:=$REPO_ROOT/dist/ValheimServerGUI-$version-x86_64.AppImage}"
: "${ARTIFACT[tarball]:=$REPO_ROOT/dist/ValheimServerGUI-$version-linux-x64.tar.gz}"
for kind in "${KINDS[@]}"; do
  [[ -f "${ARTIFACT[$kind]}" ]] || die "no $kind at ${ARTIFACT[$kind]}"
  ok "$kind: ${ARTIFACT[$kind]}"
done

section "Ubuntu image"
if [[ $REBUILD_IMAGE -eq 1 ]] || ! vol_exists "$LINUX_DESKTOP_VOL" || [[ ! -f "$LINUX_CACHE/desktop.stamp" ]] \
    || [[ "$(cut -d' ' -f3 "$LINUX_CACHE/desktop.stamp")" != "$DESKTOP_INPUTS_HASH" ]]; then
  log "building the desktop layer (cloud-init installs Ubuntu desktop; 10-20 min)"
  build_linux_desktop_layer "$DESKTOP_INPUTS_HASH" \
    || die "desktop layer build failed (VM vsg-ubuntu-build-desktop left up for inspection; the next build removes it)"
fi
ok "desktop layer from $(date -d "@$(cut -d' ' -f1 "$LINUX_CACHE/desktop.stamp")" +%F)"

section "Reboot sequence in Ubuntu VMs (${KINDS[*]})"
for kind in "${KINDS[@]}"; do
  ( run_kind "$kind" >"$RUNLOG/vm-$kind.log" 2>&1; echo $? >"$RUNLOG/exit-$kind" ) &
done
wait

FAILED=()
for kind in "${KINDS[@]}"; do
  section "$kind"
  sed 's/^/  /' "$RUNLOG/vm-$kind.log"
  [[ "$(cat "$RUNLOG/exit-$kind" 2>/dev/null)" == 0 ]] || FAILED+=("$kind")
done

section "Summary"
for kind in "${KINDS[@]}"; do
  if [[ " ${FAILED[*]} " == *" $kind "* ]]; then status=fail; else status=pass; fi
  printf '%-8s : %s  (results: %s)\n' "$kind" "$status" "$ARTIFACT_ROOT/$kind"
  [[ $KEEP -eq 1 ]] && printf '           kept VM vsg-reboot-linux-%s-%s at %s (ssh -i %s %s@<ip>)\n' \
    "$kind" "$$" "$(cat "$RUNLOG/ip-$kind" 2>/dev/null)" "$KEY" "$VM_USER"
done
printf 'run log  : %s\n' "$RUNLOG"
[[ ${#FAILED[@]} -eq 0 ]] || exit 1
echo "ALL CLEAR"
