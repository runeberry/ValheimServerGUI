# shellcheck shell=bash
#
# linux-vm.sh — the Ubuntu 24.04 desktop VM image and guest helpers for validate-reboot-linux.sh. Sourced after
# libvirt-vm.sh, never run.
#
# Layers in the libvirt pool: Ubuntu's cloud image (tests/reboot-vm/linux.env) and, on top of it, a desktop layer
# configured by cloud-init on its first boot (tests/reboot-vm/user-data.yaml): GNOME on Wayland with auto-logon,
# the .NET runtime from the Ubuntu archive, AT-SPI bindings and a canary autostart entry.
#
# The caller sets RUNLOG (a log directory) before building images.

LINUX_VM_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../tests/reboot-vm" && pwd)"
LINUX_CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/valheim-server-gui/linux"
LINUX_DESKTOP_VOL=vsg-ubuntu-desktop.qcow2
LINUX_POWEROFF="sudo systemctl poweroff"

LINUX_BUILD_TIMEOUT=2400  # first boot: cloud-init installs the desktop, then powers off
LINUX_BOOT_TIMEOUT=300    # boot of an installed image until SSH answers

mkdir -p "$LINUX_CACHE"

define_linux_vm() { # name, then the --disk and other virt-install arguments
  virt-install --connect "$LIBVIRT_URI" --name "$1" --import "${@:2}" \
    --memory 4096 --vcpus 4 --cpu host-passthrough --machine q35 \
    --network network=default,model=virtio --graphics vnc,listen=127.0.0.1 --video virtio \
    --osinfo ubuntu24.04 --noautoconsole >/dev/null
}

# Runs a command as the VM user inside the graphical session's environment (display, session bus), as an app
# launched from the desktop would see it. The command is a single shell string.
vm_session() { # ip command
  vm_ssh "$1" "export XDG_RUNTIME_DIR=/run/user/\$(id -u); \
    eval \"\$(systemctl --user show-environment | grep -E '^(DISPLAY|XAUTHORITY|WAYLAND_DISPLAY|DBUS_SESSION_BUS_ADDRESS|XDG_SESSION_TYPE|XDG_CURRENT_DESKTOP)=' | sed 's/^/export /')\"; $2"
}

# The user is logged on to GNOME once the session has published its display to the user manager.
wait_graphical_session() { # ip
  local deadline=$((SECONDS + 180))
  while ((SECONDS < deadline)); do
    # shellcheck disable=SC2016  # expands in the guest
    vm_session "$1" '[ -n "${DISPLAY:-}" ] && pgrep -u "$(id -u)" -x gnome-shell >/dev/null' 2>/dev/null && return 0
    sleep 5
  done
  return 1
}

# Reboots the guest and echoes its address once SSH answers and the user is logged on again.
reboot_linux_vm() { # domain ip
  local ip
  vm_ssh "$2" "sudo systemctl reboot" >/dev/null 2>&1
  sleep 15
  ip="$(wait_ssh "$1" "$LINUX_BOOT_TIMEOUT")" || return 1
  wait_graphical_session "$ip" || return 1
  echo "$ip"
}

ensure_ubuntu_image() { # sets UBUNTU_VOL
  # shellcheck source=tests/reboot-vm/linux.env
  source "$LINUX_VM_DIR/linux.env"
  UBUNTU_VOL="vsg-ubuntu-2404-cloud-${UBUNTU_IMAGE_SHA256:0:12}.qcow2"
  vol_exists "$UBUNTU_VOL" && return 0
  local img="$LINUX_CACHE/ubuntu-cloud.img"
  log "downloading the Ubuntu 24.04 cloud image (~600 MB)"
  curl -fsSL --retry 5 -o "$img" "$UBUNTU_IMAGE_URL" || return 1
  echo "$UBUNTU_IMAGE_SHA256  $img" | sha256sum -c --quiet - || { rm -f "$img"; return 1; }
  # The cloud image is a qcow2 file; upload it as one so overlays can use it as their backing volume.
  vol_delete "$UBUNTU_VOL"
  "${VIRSH[@]}" vol-create-as "$POOL" "$UBUNTU_VOL" "$(stat -c %s "$img")" --format qcow2 >/dev/null \
    && "${VIRSH[@]}" vol-upload --pool "$POOL" "$UBUNTU_VOL" "$img"
  local rc=$?
  rm -f "$img"
  return $rc
}

build_linux_desktop_layer() { # inputs-hash
  local dom=vsg-ubuntu-build-desktop t0=$SECONDS userdata
  destroy_vm "$dom"
  vol_delete "$LINUX_DESKTOP_VOL"
  rm -f "$LINUX_CACHE/desktop.stamp"

  ensure_ubuntu_image || { fail "could not fetch the Ubuntu cloud image"; return 1; }
  create_overlay "$LINUX_DESKTOP_VOL" "$UBUNTU_VOL" 40G || return 1
  userdata="$RUNLOG/user-data.yaml"
  sed "s|@@SSH_KEY@@|$(cat "$KEY.pub")|" "$LINUX_VM_DIR/user-data.yaml" > "$userdata"
  define_linux_vm "$dom" --disk "vol=$POOL/$LINUX_DESKTOP_VOL,bus=virtio" --cloud-init "user-data=$userdata" || return 1
  log "cloud-init is installing the desktop (watch with: virt-viewer -c $LIBVIRT_URI $dom)"
  wait_shut_off "$dom" "$LINUX_BUILD_TIMEOUT" || { fail "the desktop build did not power off in ${LINUX_BUILD_TIMEOUT}s"; return 1; }
  "${VIRSH[@]}" undefine "$dom" >/dev/null

  # Prove the image before stamping it: it boots to an automatic GNOME login with cloud-init finished cleanly.
  define_linux_vm "$dom" --disk "vol=$POOL/$LINUX_DESKTOP_VOL,bus=virtio" || return 1
  local ip
  ip="$(wait_ssh "$dom" "$LINUX_BOOT_TIMEOUT")" || { fail "the desktop image did not reach SSH"; return 1; }
  # cloud-init is disabled from here on, so its first boot's result file is the record.
  vm_ssh "$ip" "sudo cat /var/lib/cloud/data/result.json; sudo tail -n 40 /var/log/cloud-init-output.log" >"$RUNLOG/cloud-init.log" 2>&1
  grep -qE '"errors": \[\]' "$RUNLOG/cloud-init.log" || { fail "cloud-init reported errors"; cat "$RUNLOG/cloud-init.log"; return 1; }
  wait_graphical_session "$ip" || { fail "no automatic GNOME login"; return 1; }
  shutdown_vm "$dom" "$ip" "$LINUX_POWEROFF" || { fail "the desktop build VM did not shut down"; return 1; }
  "${VIRSH[@]}" undefine "$dom" >/dev/null

  echo "$(date +%s) - $1" > "$LINUX_CACHE/desktop.stamp"
  ok "desktop layer built in $(( (SECONDS - t0) / 60 )) min"
}
