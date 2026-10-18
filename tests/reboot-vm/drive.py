#!/usr/bin/env python3
"""Drives VSG's start-with-OS setting through its UI, the way a user does, over AT-SPI.

Used by scripts/validate-reboot-linux.sh, which runs it as the VM user inside the GNOME session's environment:

    drive.py <action> <installed path>
    drive.py status <installed path>

The installed path is the AppImage file or the tarball's ValheimServerGUI.App. Actions: enable, disable (set the
checkbox either way), expect-unchecked-then-enable (fail unless the checkbox reads unchecked when the dialog
opens, then enable). Each one opens File > Preferences, sets the checkbox, clicks OK, then closes the app with
File > Close and waits for it to exit. Controls are found by AutomationId (the AT-SPI accessible id), which is not
localized, and clicked with the mouse through xdotool: the app runs under Xwayland, which takes XTEST input, while
AT-SPI's own mouse events do nothing in a Wayland session (and menu items have no AT-SPI action). Prints a log and ends with "PASS" or "FAIL: <reason>" (exit code 0 or 1).

"status" prints "canary=yes|no app=yes|no": whether the canary autostart entry has run since boot, and whether the
app is running.
"""
import os
import subprocess
import sys
import time

import gi

gi.require_version("Atspi", "2.0")
from gi.repository import Atspi, GLib  # noqa: E402

APP_BINARY = "ValheimServerGUI.App"
CANARY = os.path.expanduser("~/canary.txt")


def boot_time():
    with open("/proc/stat") as stat:
        return next(float(line.split()[1]) for line in stat if line.startswith("btime "))


def app_pids(target):
    """The app's processes for this install: the binary itself (tarball) or the one an AppImage started."""
    pids = []
    for entry in os.listdir("/proc"):
        if not entry.isdigit():
            continue
        try:
            exe = os.readlink(f"/proc/{entry}/exe")
            if os.path.basename(exe) != APP_BINARY or os.stat(f"/proc/{entry}").st_uid != os.getuid():
                continue
            with open(f"/proc/{entry}/environ", "rb") as env:
                appimage = next((v[len(b"APPIMAGE="):] for v in env.read().split(b"\0") if v.startswith(b"APPIMAGE=")), None)
        except OSError:
            continue  # gone, or not ours
        if exe == target or (appimage is not None and appimage.decode() == target):
            pids.append(int(entry))
    return pids


def status(target):
    boot = boot_time()
    try:
        with open(CANARY) as canary:
            fired = any(float(line) > boot for line in canary if line.strip())
    except OSError:
        fired = False
    print(f"canary={'yes' if fired else 'no'} app={'yes' if app_pids(target) else 'no'}")
    return 0


# ---- UI ------------------------------------------------------------------------------------------

def wait_until(probe, seconds, what):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        value = probe()
        if value:
            return value
        time.sleep(0.25)
    raise TimeoutError(f"timed out after {seconds}s waiting for {what}")


def find_app(pid):
    desktop = Atspi.get_desktop(0)
    for i in range(desktop.get_child_count()):
        app = desktop.get_child_at_index(i)
        if app is not None and app.get_process_id() == pid:
            return app
    return None


def find_by_id(pid, automation_id, role):
    """A form field wrapper and the control inside it can share an id, so the role picks the one to act on."""
    app = find_app(pid)
    if app is None:
        return None
    app.clear_cache()
    stack = [app]
    while stack:
        node = stack.pop()
        try:
            if node.get_accessible_id() == automation_id and node.get_role() == role:
                return node
            stack.extend(c for c in (node.get_child_at_index(i) for i in range(node.get_child_count())) if c)
        except GLib.Error:
            continue  # an element that went away mid-walk
    return None


def wait_by_id(pid, automation_id, role, seconds=30):
    return wait_until(lambda: find_by_id(pid, automation_id, role), seconds, f"control '{automation_id}'")


def click(node):
    """Clicks the middle of the control with the mouse, like a user."""
    box = node.get_extents(Atspi.CoordType.SCREEN)
    if box.width <= 0 or box.height <= 0:
        raise RuntimeError(f"control '{node.get_accessible_id()}' has no size (off screen?)")
    subprocess.run(["xdotool", "mousemove", str(box.x + box.width // 2), str(box.y + box.height // 2), "click", "1"],
                   check=True)


def is_checked(node):
    return node.get_state_set().contains(Atspi.StateType.CHECKED)


def open_file_menu_item(pid, item_id):
    click(wait_by_id(pid, "Menu_File", Atspi.Role.MENU_ITEM))
    click(wait_by_id(pid, item_id, Atspi.Role.MENU_ITEM, 10))


def drive(action, target):
    pids = app_pids(target)
    if pids:
        print(f"attaching to the running app (pid {pids[0]})")
    else:
        print("launching the app")
        # As the desktop launches an app: a transient unit of the user's service manager.
        subprocess.run(["systemd-run", "--user", "--collect", "--quiet", target], check=True)
        pids = wait_until(lambda: app_pids(target), 30, "the app process")
    pid = pids[0]

    # The main window, then any startup prompt (a fresh install has no server executable). "No" dismisses it.
    wait_by_id(pid, "Menu_File", Atspi.Role.MENU_ITEM, 60)
    try:
        prompt = wait_by_id(pid, "MessageBox_False", Atspi.Role.PUSH_BUTTON, 5)
        print("dismissing a startup prompt")
        click(prompt)
        time.sleep(0.5)
    except TimeoutError:
        pass

    open_file_menu_item(pid, "Menu_Preferences")
    checkbox = wait_by_id(pid, "Preferences_StartWithOs", Atspi.Role.CHECK_BOX)
    checked = is_checked(checkbox)
    print(f"checkbox on open: {'checked' if checked else 'unchecked'}")
    if action == "enable":
        want = True
    elif action == "disable":
        want = False
    elif action == "expect-unchecked-then-enable":
        if checked:
            raise RuntimeError("the checkbox opened checked, but the autostart entry is disabled")
        want = True
    else:
        raise ValueError(f"unknown action '{action}'")
    if checked != want:
        click(checkbox)
        wait_until(lambda: is_checked(checkbox) == want, 5, "the checkbox to change")
    print(f"checkbox set: {'checked' if is_checked(checkbox) else 'unchecked'}")
    click(wait_by_id(pid, "Preferences_OK", Atspi.Role.PUSH_BUTTON))
    wait_until(lambda: find_by_id(pid, "Preferences_StartWithOs", Atspi.Role.CHECK_BOX) is None, 10,
               "the Preferences dialog to close")

    open_file_menu_item(pid, "Menu_Close")
    wait_until(lambda: not os.path.exists(f"/proc/{pid}"), 30, "the app to exit")
    print("app exited")


def main():
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2
    action, target = sys.argv[1], sys.argv[2]
    if action == "status":
        return status(target)
    print(f"action: {action}  target: {target}  session: {os.environ.get('XDG_SESSION_TYPE')} display: {os.environ.get('DISPLAY')}")
    try:
        drive(action, target)
        result = "PASS"
    except Exception as error:  # report every failure the same way, as the last line
        result = f"FAIL: {error}"
    entry = os.path.expanduser("~/.config/autostart/ValheimServerGUI.desktop")
    print(f"autostart entry {entry}:")
    try:
        with open(entry) as text:
            print(text.read().rstrip())
    except OSError:
        print("  (none)")
    print(result)
    return 0 if result == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
