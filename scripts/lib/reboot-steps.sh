# shellcheck shell=bash
#
# reboot-steps.sh — the step runner and the after-reboot judgment shared by validate-reboot-windows.sh and
# validate-reboot-linux.sh. Sourced after libvirt-vm.sh, never run.
#
# Each reboot is judged against a canary, a second startup entry that runs in the same login startup pass as VSG's:
# once it has run, VSG should be running (expected launch) or should never appear (expected no launch; checked
# after a grace period). A canary that never runs fails the check, so "not launched" can never pass vacuously.
#
# The caller defines, for its OS:
#   guest_reboot     reboots the VM in $DOM at $IP and echoes its new address once the user is logged on again
#   guest_status     prints "canary=yes|no app=yes|no" for the current boot
#   guest_drive      runs one UI action (its argument) and prints the driver's log, ending in PASS or FAIL: ...
# and sets DOM, IP and STEP_DIR (where screenshots and driver logs go).

CANARY_TIMEOUT=300   # login until the canary has run
LAUNCH_TIMEOUT=180   # canary until VSG is running
GRACE=60             # canary until "not launched" is believed

STEP=0

step() { STEP=$((STEP + 1)); section "Step $STEP: $1"; }

shot() { screenshot_vm "$DOM" "$STEP_DIR/$(printf '%02d' "$STEP")-$1"; }

# Runs one driver action and reports it; the driver's log is kept per step.
drive() { # action
  local log result
  log="$STEP_DIR/$(printf '%02d' "$STEP")-$1.log"
  guest_drive "$1" >"$log" 2>&1
  result="$(tr -d '\r' <"$log" | grep -E '^(PASS|FAIL)' | tail -n 1)"
  shot "$1"
  if [[ "$result" == PASS ]]; then
    ok "step $STEP: UI $1"
    return 0
  fi
  fail "step $STEP: UI $1: ${result:-the driver gave no result (see $log)}"
  return 1
}

# Reboots, waits for the canary, then checks whether VSG was launched at login.
reboot_and_expect() { # launched|not-launched
  local t0
  # shellcheck disable=SC2034  # read by the caller's guest hooks
  IP="$(guest_reboot)" || { shot no-login; fail "step $STEP: the VM did not come back from the reboot"; return 1; }
  t0=$SECONDS
  until [[ "$(guest_status)" == canary=yes* ]]; do
    ((SECONDS - t0 > CANARY_TIMEOUT)) && { shot no-canary; fail "step $STEP: the canary startup entry never ran"; return 1; }
    sleep 5
  done
  t0=$SECONDS
  if [[ "$1" == launched ]]; then
    until [[ "$(guest_status)" == *app=yes ]]; do
      ((SECONDS - t0 > LAUNCH_TIMEOUT)) && { shot not-launched; fail "step $STEP: VSG was not launched at login"; return 1; }
      sleep 5
    done
    shot launched
    ok "step $STEP: VSG launched at login"
  else
    while ((SECONDS - t0 < GRACE)); do
      [[ "$(guest_status)" == *app=yes ]] && { shot launched; fail "step $STEP: VSG was launched at login, but should not have been"; return 1; }
      sleep 5
    done
    shot not-launched
    ok "step $STEP: VSG not launched at login"
  fi
}
