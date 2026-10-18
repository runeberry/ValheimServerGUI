# shellcheck shell=bash
#
# libvirt-vm.sh — helpers shared by the scripts that run tests in disposable libvirt VMs (integration-windows.sh,
# validate-reboot-windows.sh, validate-reboot-linux.sh). Sourced, never run.
#
# Every VM is a copy-on-write overlay of a read-only image layer in the libvirt pool, reached over SSH with one key.
# The caller may set before sourcing: VSG_LIBVIRT_URI (default qemu:///system) and VM_USER (default vsg).
# shellcheck disable=SC2034  # the variables are read by the sourcing scripts

LIBVIRT_URI="${VSG_LIBVIRT_URI:-qemu:///system}"
VIRSH=(virsh -c "$LIBVIRT_URI")
POOL=default
KEY="$HOME/.ssh/vsg_win_vm"
VM_USER="${VM_USER:-vsg}"
OVMF_CODE=/usr/share/OVMF/OVMF_CODE_4M.fd
OVMF_VARS=/usr/share/OVMF/OVMF_VARS_4M.fd

SSH_OPTS=(-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR -o ConnectTimeout=5 -o BatchMode=yes)

section() { printf '\n=== %s ===\n' "$1"; }
ok()   { printf '[ OK ] %s\n' "$1"; }
fail() { printf '[FAIL] %s\n' "$1"; }
log()  { printf '[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }
die()  { fail "$*"; exit 1; }

vm_ssh() { ssh "${SSH_OPTS[@]}" -i "$KEY" "$VM_USER@$1" "${@:2}"; }
vm_scp() { scp -q "${SSH_OPTS[@]}" -i "$KEY" "$@"; }

vol_exists() { "${VIRSH[@]}" vol-info --pool "$POOL" "$1" >/dev/null 2>&1; }
vol_delete() { "${VIRSH[@]}" vol-delete --pool "$POOL" "$1" >/dev/null 2>&1 || true; }

create_overlay() { # name backing [size]
  "${VIRSH[@]}" vol-create-as "$POOL" "$1" "${3:-128G}" --format qcow2 --backing-vol "$2" --backing-vol-format qcow2 >/dev/null
}

upload_vol() { # file name
  vol_delete "$2"
  "${VIRSH[@]}" vol-create-as "$POOL" "$2" "$(stat -c %s "$1")" --format raw >/dev/null \
    && "${VIRSH[@]}" vol-upload --pool "$POOL" "$2" "$1"
}

destroy_vm() { # name [volume]
  "${VIRSH[@]}" destroy "$1" >/dev/null 2>&1
  "${VIRSH[@]}" undefine --nvram "$1" >/dev/null 2>&1
  [[ -n "${2:-}" ]] && vol_delete "$2"
  return 0
}

# Echoes the address that answers SSH. Re-reads the DHCP leases on every try: an installer and the installed system
# can lease different addresses for the same NIC.
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

# Asks the guest to power off and waits for it to be off. Undefining a running domain would leave it running as a
# transient domain that keeps writing to the image.
shutdown_vm() { # domain ip poweroff-command
  vm_ssh "$2" "$3" >/dev/null 2>&1
  wait_shut_off "$1" 300
}

wait_shut_off() { # domain timeout
  local deadline=$((SECONDS + $2))
  while ((SECONDS < deadline)); do
    [[ "$("${VIRSH[@]}" domstate "$1" 2>/dev/null)" == "shut off" ]] && return 0
    sleep 5
  done
  return 1
}

# Saves a PNG of the VM's screen (virsh writes PPM; converted when ImageMagick or Python PIL is around).
screenshot_vm() { # domain file-without-extension
  "${VIRSH[@]}" screenshot "$1" "$2.ppm" >/dev/null 2>&1 || return 0
  if command -v convert >/dev/null 2>&1 && convert "$2.ppm" "$2.png" 2>/dev/null; then
    rm -f "$2.ppm"
  fi
  return 0
}

stamp_age_days() { # stamp file -> whole days since it was written, or a huge number when missing
  [[ -f "$1" ]] || { echo 99999; return; }
  echo $(( ($(date +%s) - $(head -1 "$1" | cut -d' ' -f1)) / 86400 ))
}

# Checks the host tools and libvirt access, and creates the SSH key on first use.
vm_preflight() { # extra required tools...
  local tool
  for tool in virsh virt-install ssh scp curl "$@"; do
    command -v "$tool" >/dev/null 2>&1 || die "$tool is required"
  done
  "${VIRSH[@]}" pool-info "$POOL" >/dev/null 2>&1 || die "cannot reach libvirt pool '$POOL' at $LIBVIRT_URI (libvirt group?)"
  [[ -f "$KEY" ]] || ssh-keygen -q -t ed25519 -N '' -C vsg-integration-vm -f "$KEY"
  # New files in ~/.ssh can inherit a group-readable default ACL; ssh then silently ignores the key.
  chmod 600 "$KEY"
}
