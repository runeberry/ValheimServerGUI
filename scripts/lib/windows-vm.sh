# shellcheck shell=bash
#
# windows-vm.sh — the Windows 11 VM image and guest helpers shared by integration-windows.sh and
# validate-reboot-windows.sh. Sourced after libvirt-vm.sh, never run.
#
# The OS layer is Windows 11 Enterprise evaluation (unattended install from Microsoft's ISO), OpenSSH, the .NET 10
# SDK, permanent auto-logon and the vsg-runner task (tests/integration-vm/). Each script adds its own layer on top
# (server, desktop); a child layer is rebuilt whenever its stamp is older than the OS layer's.
#
# The caller sets RUNLOG (a log directory) before building images.

WIN_VM_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../tests/integration-vm" && pwd)"
CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/valheim-server-gui/windows"
OS_VOL=vsg-win11-os.qcow2
ANSWER_VOL=vsg-win11-answer.iso
# Layers built on the OS layer; an OS rebuild deletes them (as "volume:stamp:build domain").
WIN_CHILD_LAYERS=(
  "vsg-win11-server.qcow2:server.stamp:vsg-win-build-server"
  "vsg-win11-desktop.qcow2:desktop.stamp:vsg-win-build-desktop"
)
WIN_POWEROFF="shutdown /s /t 0"

IMAGE_MAX_AGE_DAYS=80    # the evaluation licence expires 90 days after the install
INSTALL_TIMEOUT=5400     # unattended Windows install until SSH answers
BOOT_TIMEOUT=600         # boot of an installed image until SSH answers

mkdir -p "$CACHE"

vm_ps() { vm_ssh "$1" "powershell -NoProfile -ExecutionPolicy Bypass -Command \"$2\""; }

define_vm() { # name, then the --disk arguments
  virt-install --connect "$LIBVIRT_URI" --name "$1" --import "${@:2}" \
    --memory 8192 --vcpus 4 --cpu host-passthrough --machine q35 \
    --boot "loader=$OVMF_CODE,loader.readonly=yes,loader.type=pflash,nvram.template=$OVMF_VARS" \
    --network network=default,model=e1000e --graphics vnc,listen=127.0.0.1 --video qxl \
    --osinfo win11 --tpm none --noautoconsole >/dev/null
}

# The user must hold the console session (session 1) for the /it tasks to start. sshd starts a little before
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

# Reboots the guest and echoes its address once SSH answers and the user is logged on again.
reboot_vm() { # domain ip
  local ip
  vm_ssh "$2" "shutdown /r /t 0" >/dev/null 2>&1
  # Long enough that the old sshd is gone, so wait_ssh cannot answer from the system going down.
  sleep 20
  ip="$(wait_ssh "$1" "$BOOT_TIMEOUT")" || return 1
  wait_interactive_session "$ip" >/dev/null || return 1
  echo "$ip"
}

ensure_iso() { # sets ISO_VOL
  # shellcheck source=tests/integration-vm/image.env
  source "$WIN_VM_DIR/image.env"
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
  sed "s|@@PASSWORD@@|$1|g" "$WIN_VM_DIR/autounattend.xml" > "$dir/autounattend.xml"
  cp "$WIN_VM_DIR/firstlogon.ps1" "$dir/"
  cp "$KEY.pub" "$dir/authorized_key.pub"
  xorriso -as mkisofs -quiet -J -r -V VSG_ANSWER -o "$dir.iso" "$dir" 2>/dev/null \
    && upload_vol "$dir.iso" "$ANSWER_VOL" >/dev/null
  local rc=$?
  rm -rf "$dir" "$dir.iso"
  return $rc
}

build_os_layer() {
  local dom=vsg-win-build-os password ip t0=$SECONDS layer
  for layer in "${WIN_CHILD_LAYERS[@]}"; do
    IFS=: read -r vol stamp builder <<<"$layer"
    destroy_vm "$builder"
    vol_delete "$vol"
    rm -f "$CACHE/$stamp"
  done
  destroy_vm "$dom"
  vol_delete "$OS_VOL"
  rm -f "$CACHE/os.stamp"

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

  vm_scp "$WIN_VM_DIR/bake-os.ps1" "$VM_USER@$ip:C:/bake-os.ps1" || return 1
  vm_ssh "$ip" "powershell -NoProfile -ExecutionPolicy Bypass -File C:\\bake-os.ps1 -Password $password" >"$RUNLOG/bake-os.log" 2>&1 \
    || { fail "bake-os.ps1"; tail -n 30 "$RUNLOG/bake-os.log"; return 1; }
  vm_ssh "$ip" "del C:\\bake-os.ps1" >/dev/null

  # Auto-logon must hold across boots, or every run fails at "no interactive session".
  ip="$(reboot_vm "$dom" "$ip")" || { fail "auto-logon did not hold after a reboot"; return 1; }

  shutdown_vm "$dom" "$ip" "$WIN_POWEROFF" || { fail "the OS build VM did not shut down"; return 1; }
  "${VIRSH[@]}" undefine --nvram "$dom" >/dev/null
  vol_delete "$ANSWER_VOL"
  date +%s > "$CACHE/os.stamp"
  ok "OS layer built in $(( (SECONDS - t0) / 60 )) min"
}

# Builds the OS layer when missing, too old for the evaluation licence, or when asked; sets OS_AGE_DAYS.
ensure_os_image() { # rebuild(0|1)
  OS_AGE_DAYS="$(stamp_age_days "$CACHE/os.stamp")"
  if [[ "$1" -eq 1 ]] || ! vol_exists "$OS_VOL" || ((OS_AGE_DAYS >= IMAGE_MAX_AGE_DAYS)); then
    log "building the OS layer (unattended Windows install, 30-60 min)"
    build_os_layer || die "OS layer build failed (VM vsg-win-build-os left up for inspection; the next build removes it)"
    OS_AGE_DAYS=0
  fi
}

# True when a child layer must be (re)built: missing, never stamped, older than the OS layer, or built from other
# inputs (the stamp's third field).
child_layer_stale() { # volume stamp-file inputs-hash
  ! vol_exists "$1" || [[ ! -f "$2" ]] || [[ "$2" -ot "$CACHE/os.stamp" ]] || [[ "$(cut -d' ' -f3 "$2")" != "$3" ]]
}
