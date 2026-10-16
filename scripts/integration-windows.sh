#!/usr/bin/env bash
#
# integration-windows.sh — the COMPLETE test suite on Windows: the build + unit tests (scripts/validate.sh --quick)
# on the host, then the whole solution with -p:Integration=true inside a disposable Windows 11 VM per server
# configuration, in parallel. The Windows counterpart of scripts/integration.sh (docs/WINDOWS_INTEGRATION_PLAN.md).
#
# Every run boots a fresh VM from a copy-on-write overlay of a read-only image and deletes it afterwards, so no run
# sees another's state. The image has two layers, like the container image:
#   OS layer      Windows 11 Enterprise evaluation (unattended install from Microsoft's ISO), OpenSSH, .NET 10
#                 SDK, permanent auto-logon, the vsg-runner task. Rebuilt every IMAGE_MAX_AGE_DAYS (the evaluation
#                 licence lasts 90 days from install) or on --rebuild-image. Takes 30-60 minutes, unattended.
#   server layer  steamcmd + C:\valheim\vanilla + C:\valheim\bepinex (pins from
#                 tests/integration-image/server-configs.env). Rebuilt with the OS layer, on --update-server, or
#                 when server-configs.env or bake-server.ps1 changes.
#
# Why a VM and a scheduled task: the lifecycle tests stop the server gracefully, and Windows refuses a graceful stop
# from session 0, where SSH lands. The VM logs its user on automatically, and the host starts the suite with
# `schtasks /run` on a task that Windows only runs in that interactive session (session 1). --control runs the suite
# over SSH instead and passes only if the lifecycle tests FAIL there, which proves the session is what matters.
#
# Needs libvirt/KVM (membership of the libvirt group, no sudo), xorriso, ssh, and outbound internet (ISO, .NET,
# Steam). The first run downloads the 7 GB ISO. Images live in the libvirt pool `default`; the ISO cache, image
# stamps and the VM user's password live in ~/.cache/valheim-server-gui/windows. The VMs sit on libvirt's NAT
# network. Domains and volumes this script creates are named vsg-it-*, vsg-win-build-* and vsg-win11-*; nothing
# else is touched.
#
# Usage: integration-windows.sh [--rebuild-image] [--update-server] [--keep] [--control <config>] [config...]
#   --rebuild-image     rebuild the OS layer (and the server layer on top of it)
#   --update-server     rebuild only the server layer (picks up a new Valheim build)
#   --keep              leave the run VMs up afterwards (prints how to reach and remove them)
#   --control <config>  negative control: run the suite over SSH in session 0; passes only if lifecycle tests fail
#   config...           run only these configurations (default: all)
#
# Keep this script + tests/integration-vm/* + the CLAUDE.local.md note in step with the test project as the
# workflow evolves (same rule as validate.sh and integration.sh).

set -uo pipefail

ALL_CONFIGS=(vanilla bepinex)

REBUILD_IMAGE=0
UPDATE_SERVER=0
KEEP=0
CONTROL=""
CONFIGS=()
usage() { echo "usage: $0 [--rebuild-image] [--update-server] [--keep] [--control <config>] [${ALL_CONFIGS[*]}]..." >&2; exit 2; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --rebuild-image) REBUILD_IMAGE=1 ;;
    --update-server) UPDATE_SERVER=1 ;;
    --keep) KEEP=1 ;;
    --control) [[ "${2:-}" =~ ^(vanilla|bepinex)$ ]] || usage; CONTROL="$2"; shift ;;
    vanilla|bepinex) CONFIGS+=("$1") ;;
    *) usage ;;
  esac
  shift
done
[[ ${#CONFIGS[@]} -eq 0 ]] && CONFIGS=("${ALL_CONFIGS[@]}")
[[ -n "$CONTROL" ]] && CONFIGS=("$CONTROL")

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VM_DIR="$REPO_ROOT/tests/integration-vm"
PINS_FILE="$REPO_ROOT/tests/integration-image/server-configs.env"
# What the server layer is built from besides the Valheim build; recorded in its stamp so a change rebuilds it.
SERVER_INPUTS_HASH="$(cat "$PINS_FILE" "$VM_DIR/bake-server.ps1" | sha256sum | cut -c1-16)"
ARTIFACT_ROOT="$REPO_ROOT/dist/integration-windows"
CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/valheim-server-gui/windows"
LIBVIRT_URI="${VSG_LIBVIRT_URI:-qemu:///system}"
VIRSH=(virsh -c "$LIBVIRT_URI")
POOL=default
OS_VOL=vsg-win11-os.qcow2
SERVER_VOL=vsg-win11-server.qcow2
ANSWER_VOL=vsg-win11-answer.iso
KEY="$HOME/.ssh/vsg_win_vm"
VM_USER=vsg
OVMF_CODE=/usr/share/OVMF/OVMF_CODE_4M.fd
OVMF_VARS=/usr/share/OVMF/OVMF_VARS_4M.fd

IMAGE_MAX_AGE_DAYS=80    # the evaluation licence expires 90 days after the install
INSTALL_TIMEOUT=5400     # unattended Windows install until SSH answers
BOOT_TIMEOUT=600         # boot of an installed image until SSH answers
RUN_TIMEOUT=1500         # one configuration's whole suite (in-VM blame-hang is 360 s per test)

SSH_OPTS=(-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR -o ConnectTimeout=5 -o BatchMode=yes)

section() { printf '\n=== %s ===\n' "$1"; }
ok()   { printf '[ OK ] %s\n' "$1"; }
fail() { printf '[FAIL] %s\n' "$1"; }
log()  { printf '[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }
die()  { fail "$*"; exit 1; }

vm_ssh() { ssh "${SSH_OPTS[@]}" -i "$KEY" "$VM_USER@$1" "${@:2}"; }
vm_ps()  { vm_ssh "$1" "powershell -NoProfile -ExecutionPolicy Bypass -Command \"$2\""; }
vm_scp() { scp -q "${SSH_OPTS[@]}" -i "$KEY" "$@"; }

vol_exists() { "${VIRSH[@]}" vol-info --pool "$POOL" "$1" >/dev/null 2>&1; }
vol_delete() { "${VIRSH[@]}" vol-delete --pool "$POOL" "$1" >/dev/null 2>&1 || true; }

create_overlay() { # name backing
  "${VIRSH[@]}" vol-create-as "$POOL" "$1" 128G --format qcow2 --backing-vol "$2" --backing-vol-format qcow2 >/dev/null
}

upload_vol() { # file name
  vol_delete "$2"
  "${VIRSH[@]}" vol-create-as "$POOL" "$2" "$(stat -c %s "$1")" --format raw >/dev/null \
    && "${VIRSH[@]}" vol-upload --pool "$POOL" "$2" "$1"
}

define_vm() { # name, then the --disk arguments
  virt-install --connect "$LIBVIRT_URI" --name "$1" --import "${@:2}" \
    --memory 8192 --vcpus 4 --cpu host-passthrough --machine q35 \
    --boot "loader=$OVMF_CODE,loader.readonly=yes,loader.type=pflash,nvram.template=$OVMF_VARS" \
    --network network=default,model=e1000e --graphics vnc,listen=127.0.0.1 --video qxl \
    --osinfo win11 --tpm none --noautoconsole >/dev/null
}

destroy_vm() { # name [volume]
  "${VIRSH[@]}" destroy "$1" >/dev/null 2>&1
  "${VIRSH[@]}" undefine --nvram "$1" >/dev/null 2>&1
  [[ -n "${2:-}" ]] && vol_delete "$2"
  return 0
}

# Echoes the address that answers SSH. Re-reads the DHCP leases on every try: the installer's WinPE and the installed
# system can lease different addresses for the same NIC.
wait_ssh() { # domain timeout
  local deadline=$((SECONDS + $2)) ip
  while ((SECONDS < deadline)); do
    for ip in $("${VIRSH[@]}" domifaddr "$1" --source lease 2>/dev/null | grep -oE '([0-9]+\.){3}[0-9]+'); do
      vm_ssh "$ip" "echo ok" >/dev/null 2>&1 && { echo "$ip"; return 0; }
    done
    sleep 10
  done
  return 1
}

# Shuts the guest down and waits for it to be off. Undefining a running domain would leave it running as a transient
# domain that keeps writing to the image.
shutdown_vm() { # domain ip
  vm_ssh "$2" "shutdown /s /t 0" >/dev/null 2>&1
  local deadline=$((SECONDS + 300))
  while ((SECONDS < deadline)); do
    [[ "$("${VIRSH[@]}" domstate "$1" 2>/dev/null)" == "shut off" ]] && return 0
    sleep 5
  done
  return 1
}

# The user must hold the console session (session 1) for the runner task to start. sshd starts a little before
# auto-logon completes, so wait for it.
wait_interactive_session() { # ip
  local deadline=$((SECONDS + 120)) users
  while ((SECONDS < deadline)); do
    # `query user` exits 1 even when it lists sessions, so read its output instead of piping (pipefail).
    users="$(vm_ssh "$1" "query user" 2>/dev/null)"
    grep -qE "^ *>?$VM_USER +console +1 +Active" <<<"$users" && return 0
    sleep 5
  done
  echo "$users"
  return 1
}

stamp_age_days() { # stamp file -> whole days since it was written, or a huge number when missing
  [[ -f "$1" ]] || { echo 99999; return; }
  echo $(( ($(date +%s) - $(head -1 "$1" | cut -d' ' -f1)) / 86400 ))
}

# ---- image builds ----------------------------------------------------------------------------

ensure_iso() { # sets ISO_VOL
  # shellcheck source=tests/integration-vm/image.env
  source "$VM_DIR/image.env"
  ISO_VOL="vsg-win11-eval-${WIN_ISO_SHA256:0:12}.iso"
  vol_exists "$ISO_VOL" && return 0
  local iso="$CACHE/win11-eval.iso"
  if [[ ! -f "$iso" ]]; then
    log "downloading the Windows 11 evaluation ISO (~7 GB)"
    curl -fsSL --retry 5 -C - -o "$iso.part" "$WIN_ISO_URL" || return 1
    mv "$iso.part" "$iso"
  fi
  log "verifying the ISO"
  echo "$WIN_ISO_SHA256  $iso" | sha256sum -c --quiet - || { rm -f "$iso"; return 1; }
  log "uploading the ISO to the libvirt pool"
  upload_vol "$iso" "$ISO_VOL" && rm -f "$iso"
}

build_answer_iso() { # password
  local dir; dir="$(mktemp -d)"
  sed "s|@@PASSWORD@@|$1|g" "$VM_DIR/autounattend.xml" > "$dir/autounattend.xml"
  cp "$VM_DIR/firstlogon.ps1" "$dir/"
  cp "$KEY.pub" "$dir/authorized_key.pub"
  xorriso -as mkisofs -quiet -J -r -V VSG_ANSWER -o "$dir.iso" "$dir" 2>/dev/null \
    && upload_vol "$dir.iso" "$ANSWER_VOL" >/dev/null
  local rc=$?
  rm -rf "$dir" "$dir.iso"
  return $rc
}

build_os_layer() {
  local dom=vsg-win-build-os password ip t0=$SECONDS
  destroy_vm vsg-win-build-server
  destroy_vm "$dom"
  vol_delete "$SERVER_VOL"
  vol_delete "$OS_VOL"
  rm -f "$CACHE/os.stamp" "$CACHE/server.stamp"

  ensure_iso || return 1
  password="$(tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24)"
  (umask 077; echo "$password" > "$CACHE/vm-password")
  build_answer_iso "$password" || return 1

  "${VIRSH[@]}" vol-create-as "$POOL" "$OS_VOL" 128G --format qcow2 >/dev/null || return 1
  define_vm "$dom" \
    --disk "vol=$POOL/$OS_VOL,bus=sata,boot.order=1" \
    --disk "vol=$POOL/$ISO_VOL,device=cdrom,bus=sata,boot.order=2" \
    --disk "vol=$POOL/$ANSWER_VOL,device=cdrom,bus=sata" || return 1
  # The empty disk falls through to the DVD, whose boot loader waits a few seconds for "press any key" right after
  # power-on. Press only during that window: Setup's first screen has a focused Cancel button.
  for _ in $(seq 1 10); do "${VIRSH[@]}" send-key "$dom" KEY_ENTER >/dev/null 2>&1; sleep 1; done
  log "unattended install running (watch with: virt-viewer -c $LIBVIRT_URI $dom)"
  ip="$(wait_ssh "$dom" "$INSTALL_TIMEOUT")" || { fail "install did not reach SSH in ${INSTALL_TIMEOUT}s (C:\\firstlogon.log in the VM)"; return 1; }
  log "installed in $(( (SECONDS - t0) / 60 )) min; configuring"

  vm_scp "$VM_DIR/bake-os.ps1" "$VM_USER@$ip:C:/bake-os.ps1" || return 1
  vm_ssh "$ip" "powershell -NoProfile -ExecutionPolicy Bypass -File C:\\bake-os.ps1 -Password $password" >"$RUNLOG/bake-os.log" 2>&1 \
    || { fail "bake-os.ps1"; tail -n 30 "$RUNLOG/bake-os.log"; return 1; }
  vm_ssh "$ip" "del C:\\bake-os.ps1" >/dev/null

  # Auto-logon must hold across boots, or every run fails at "no interactive session".
  vm_ssh "$ip" "shutdown /r /t 0" >/dev/null 2>&1
  sleep 20
  ip="$(wait_ssh "$dom" "$BOOT_TIMEOUT")" || { fail "no SSH after the reboot"; return 1; }
  wait_interactive_session "$ip" || { fail "auto-logon did not hold after a reboot"; return 1; }

  shutdown_vm "$dom" "$ip" || { fail "the OS build VM did not shut down"; return 1; }
  "${VIRSH[@]}" undefine --nvram "$dom" >/dev/null
  vol_delete "$ANSWER_VOL"
  date +%s > "$CACHE/os.stamp"
  ok "OS layer built in $(( (SECONDS - t0) / 60 )) min"
}

build_server_layer() {
  local dom=vsg-win-build-server ip buildid t0=$SECONDS
  destroy_vm "$dom"
  vol_delete "$SERVER_VOL"
  rm -f "$CACHE/server.stamp"

  create_overlay "$SERVER_VOL" "$OS_VOL" || return 1
  define_vm "$dom" --disk "vol=$POOL/$SERVER_VOL,bus=sata" || return 1
  ip="$(wait_ssh "$dom" "$BOOT_TIMEOUT")" || { fail "the server build VM did not reach SSH"; return 1; }

  # shellcheck source=tests/integration-image/server-configs.env
  source "$PINS_FILE"
  vm_scp "$VM_DIR/bake-server.ps1" "$VM_USER@$ip:C:/bake-server.ps1" || return 1
  vm_ssh "$ip" "powershell -NoProfile -ExecutionPolicy Bypass -File C:\\bake-server.ps1 \
    -BepInExPackUrl \"$BEPINEX_PACK_URL\" -BepInExPackSha256 \"$BEPINEX_PACK_SHA256\" \
    -Plugins \"$PLUGINS\"" >"$RUNLOG/bake-server.log" 2>&1 \
    || { fail "bake-server.ps1"; tail -n 30 "$RUNLOG/bake-server.log"; return 1; }
  vm_ssh "$ip" "del C:\\bake-server.ps1" >/dev/null
  buildid="$(vm_ssh "$ip" "type C:\\valheim\\buildid.txt" | tr -dc '0-9')"

  shutdown_vm "$dom" "$ip" || { fail "the server build VM did not shut down"; return 1; }
  "${VIRSH[@]}" undefine --nvram "$dom" >/dev/null
  echo "$(date +%s) $buildid $SERVER_INPUTS_HASH" > "$CACHE/server.stamp"
  ok "server layer built in $(( (SECONDS - t0) / 60 )) min (server build $buildid)"
}

# ---- one configuration ---------------------------------------------------------------------

push_source() { # ip config
  vm_ps "$1" "Remove-Item -Recurse -Force C:\\vsg\\src,C:\\vsg\\out -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force C:\\vsg\\src | Out-Null" || return 1
  # m: extraction time as mtime, so MSBuild never mistakes new sources for older than earlier outputs.
  (cd "$REPO_ROOT" && git ls-files --cached --others --exclude-standard -z | tar czf - --null -T -) \
    | vm_ssh "$1" "tar xzmf - -C C:/vsg/src" || return 1
  vm_ps "$1" "Set-Content C:\\vsg\\config.txt '$2'"
}

collect() { # ip config
  local dest="$ARTIFACT_ROOT/$2"
  rm -rf "$dest"; mkdir -p "$dest"
  vm_scp -r "$VM_USER@$1:C:/vsg/out/*" "$dest/" 2>/dev/null
  chmod -R a+rX "$dest" 2>/dev/null
}

# Boots one configuration's VM, runs the suite and collects the artifacts; returns the suite's exit code.
run_config_in_vm() { # config domain
  local cfg="$1" dom="$2" ip code t0
  if ! create_overlay "$dom.qcow2" "$SERVER_VOL" || ! define_vm "$dom" --disk "vol=$POOL/$dom.qcow2,bus=sata"; then
    log "could not create the VM"
    return 1
  fi
  ip="$(wait_ssh "$dom" "$BOOT_TIMEOUT")" || { log "no SSH"; return 1; }
  log "$dom up at $ip"
  echo "$ip" > "$RUNLOG/ip-$cfg"
  push_source "$ip" "$cfg" || { log "could not push the source"; return 1; }

  t0=$SECONDS
  if [[ -n "$CONTROL" ]]; then
    log "negative control: suite over SSH (session 0)"
    vm_ssh "$ip" "powershell -NoProfile -ExecutionPolicy Bypass -File C:\\vsg\\src\\tests\\integration-vm\\run.ps1 -AllowServicesSession" >/dev/null 2>&1
  else
    wait_interactive_session "$ip" || { log "no interactive session for $VM_USER"; return 1; }
    vm_ssh "$ip" "schtasks /run /tn vsg-runner" >/dev/null || { log "schtasks /run failed"; return 1; }
    log "suite started in session 1"
    until vm_ps "$ip" "Test-Path C:\\vsg\\out\\exit_code.txt" 2>/dev/null | grep -q True; do
      ((SECONDS - t0 > RUN_TIMEOUT)) && { log "no result after ${RUN_TIMEOUT}s"; collect "$ip" "$cfg"; return 1; }
      sleep 15
    done
  fi
  log "suite finished in $((SECONDS - t0))s"
  collect "$ip" "$cfg"
  code="$(tr -dc '0-9' < "$ARTIFACT_ROOT/$cfg/exit_code.txt" 2>/dev/null)"
  return "${code:-1}"
}

# Runs in the background, one per configuration; writes its exit code to $RUNLOG/exit-<config>.
run_config() { # config domain
  run_config_in_vm "$1" "$2" >"$RUNLOG/vm-$1.log" 2>&1
  echo $? > "$RUNLOG/exit-$1"
}

report() { # config
  local cfg="$1" code log="$ARTIFACT_ROOT/$1/run.log"
  code="$(cat "$RUNLOG/exit-$cfg" 2>/dev/null || echo 1)"
  grep -aE 'Passed!|Failed!' "$log" 2>/dev/null | sed "s/^/  $cfg: /"
  if [[ -n "$CONTROL" ]]; then
    if [[ "$code" != 0 ]] && grep -aqE 'did not stop cleanly|did not reach Stopped' "$log" 2>/dev/null; then
      ok "$cfg: negative control failed the graceful-stop tests in session 0, as it must"
      return 0
    fi
    fail "$cfg: negative control did NOT fail the graceful-stop tests over SSH; a green run proves nothing"
  elif [[ "$code" == 0 ]]; then
    ok "$cfg: complete suite passed"
    return 0
  elif [[ -f "$log" ]]; then
    fail "$cfg: tests failed (exit $code)"
    grep -aE -A1 '\[FAIL\]|Error Message|^ERROR' "$log" | grep -vE '^--$|Stack Trace' | head -40
  else
    fail "$cfg: the suite did not run"
  fi
  sed 's/^/  | /' "$RUNLOG/vm-$cfg.log"
  echo "  (artifacts: $ARTIFACT_ROOT/$cfg)"
  return 1
}

# ---- main ------------------------------------------------------------------------------------

for tool in virsh virt-install xorriso ssh scp curl; do
  command -v "$tool" >/dev/null 2>&1 || die "$tool is required"
done
"${VIRSH[@]}" pool-info "$POOL" >/dev/null 2>&1 || die "cannot reach libvirt pool '$POOL' at $LIBVIRT_URI (libvirt group?)"
[[ -f "$KEY" ]] || ssh-keygen -q -t ed25519 -N '' -C vsg-integration-vm -f "$KEY"
# New files in ~/.ssh can inherit a group-readable default ACL; ssh then silently ignores the key.
chmod 600 "$KEY"
mkdir -p "$CACHE"

RUNLOG="$(mktemp -d "${TMPDIR:-/tmp}/vsg-integration-windows.XXXXXX")"
declare -A DOMAINS
for cfg in "${CONFIGS[@]}"; do DOMAINS[$cfg]="vsg-it-$cfg-$$"; done

cleanup() {
  [[ $KEEP -eq 1 ]] && return
  for cfg in "${CONFIGS[@]}"; do destroy_vm "${DOMAINS[$cfg]}" "${DOMAINS[$cfg]}.qcow2"; done
}
trap cleanup EXIT INT TERM

section "Build + unit tests (validate.sh --quick)"
if "$REPO_ROOT/scripts/validate.sh" --quick >"$RUNLOG/validate.log" 2>&1; then
  ok "build clean, unit tests passed"
else
  fail "validate.sh --quick failed; not starting the Windows run"
  tail -n 30 "$RUNLOG/validate.log"
  exit 1
fi

section "Windows image"
os_age="$(stamp_age_days "$CACHE/os.stamp")"
if [[ $REBUILD_IMAGE -eq 1 ]] || ! vol_exists "$OS_VOL" || ((os_age >= IMAGE_MAX_AGE_DAYS)); then
  log "building the OS layer (unattended Windows install, 30-60 min)"
  build_os_layer || die "OS layer build failed (VM vsg-win-build-os left up for inspection; the next build removes it)"
  os_age=0
fi
if [[ $UPDATE_SERVER -eq 1 ]] || ! vol_exists "$SERVER_VOL" || [[ ! -f "$CACHE/server.stamp" ]] \
    || [[ "$CACHE/server.stamp" -ot "$CACHE/os.stamp" ]] \
    || [[ "$(cut -d' ' -f3 "$CACHE/server.stamp")" != "$SERVER_INPUTS_HASH" ]]; then
  log "building the server layer"
  build_server_layer || die "server layer build failed (VM vsg-win-build-server left up for inspection; the next build removes it)"
fi
ok "OS layer $os_age days old (evaluation expires in $((90 - os_age)) days; rebuilt at $IMAGE_MAX_AGE_DAYS)"
ok "server build $(cut -d' ' -f2 "$CACHE/server.stamp")"

section "Suite in Windows VMs (${CONFIGS[*]})"
for cfg in "${CONFIGS[@]}"; do
  run_config "$cfg" "${DOMAINS[$cfg]}" &
done
wait

FAILED=()
for cfg in "${CONFIGS[@]}"; do
  report "$cfg" || FAILED+=("$cfg")
done

section "Summary"
for cfg in "${CONFIGS[@]}"; do
  if [[ " ${FAILED[*]} " == *" $cfg "* ]]; then status="fail"; else status="pass"; fi
  printf '%-8s : %s  (artifacts: %s)\n' "$cfg" "$status" "$ARTIFACT_ROOT/$cfg"
done
printf 'run log  : %s\n' "$RUNLOG"
if [[ $KEEP -eq 1 ]]; then
  for cfg in "${CONFIGS[@]}"; do
    printf 'kept VM  : %s at %s (ssh -i %s %s@<ip>; remove: virsh -c %s destroy %s; virsh -c %s undefine --nvram %s; virsh -c %s vol-delete --pool %s %s.qcow2)\n' \
      "${DOMAINS[$cfg]}" "$(cat "$RUNLOG/ip-$cfg" 2>/dev/null)" "$KEY" "$VM_USER" \
      "$LIBVIRT_URI" "${DOMAINS[$cfg]}" "$LIBVIRT_URI" "${DOMAINS[$cfg]}" "$LIBVIRT_URI" "$POOL" "${DOMAINS[$cfg]}"
  done
fi

if [[ ${#FAILED[@]} -gt 0 ]]; then
  exit 1
fi
echo "ALL CLEAR"
