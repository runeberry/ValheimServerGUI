# Plan: Windows live integration tier (`scripts/integration-windows.sh`)

Status: implemented (2026-10-15): `scripts/integration-windows.sh` + `tests/integration-vm/`. Productionizes the spike in `./tmp/win-spike/` (gitignored).

## Goal

Run the complete suite (unit + live integration, `-p:Integration=true`) on Windows, per server configuration
(`vanilla`, `bepinex`), from this Linux host with one script, the same way `scripts/integration.sh` does it for
Linux in containers. No dependency on Nuffle's laptop, no real Valheim data or profile touched.

## Constraints that shape the design (all proven in the spike)

- **Graceful stop needs an interactive session.** `taskkill` without `/f` from session 0 (SSH, WinRM, services)
  fails with "can only be terminated forcefully". The suite must run in the logged-on user's session 1.
  - Mechanism: permanent auto-logon + a scheduled task created with `/it` (interactive only), triggered from SSH
    with `schtasks /run`. The task runs in session 1 even though the trigger comes from session 0.
  - Negative control proven: same suite run directly over SSH fails 3 lifecycle tests ("server did not stop
    cleanly", "did not reach Stopped within the graceful-stop timeout").
- **Windows containers are out** (no interactive session). Full VM on libvirt/KVM is the only fit on this host.
- **One VM per configuration**, disposable qcow2 overlay on a read-only golden image, both in parallel.
- **The server needs no extra runtimes to boot** (no DirectX) on a clean Win11 Enterprise eval, but **crossplay needs the
  VC++ Redistributable**: PlayFab Party's PartyWin32.dll fails to load without it (found 2026-10-16 when the suite
  switched to crossplay). `bake-server.ps1` installs it, checked by Microsoft signature.
- **BepInEx on Windows never writes plugin lines to stdout** (its console logger writes to `CONOUT$`); the mod-loader
  test reads `BepInEx/LogOutput.log` (commit 3af9913). Nothing Windows-specific left in the tests.
- **Eval license: 90 days counted from the image install**, 1 rearm. Disposable clones inherit the golden clock.
- Measured: boot to SSH ~20 s; full suite per config 2.5-3 min; golden layer 6.9 GB on top of a 20 GB OS base;
  per-run overlay ~7.5 GB while it exists; auto-logon survives 3 consecutive reboots.

## Decisions

1. **Image source: Microsoft's Win11 Enterprise evaluation ISO + our own `autounattend.xml`, installed with
   `virt-install`. No Packer, no Vagrant.**
   - Spike used the community box `gusztavvargadr/windows-11`; fine for proving, not something to depend on (third
     party, changes OS settings we don't control, password `vagrant`).
   - Packer would add a tool + HCL for portability to other hypervisors we don't need (no hypothetical
     requirements). The install is: boot ISO + answer-file CD, wait for SSH, run bake scripts. That is ~40 lines of
     bash in the style of the existing scripts.
   - Rejected Windows Server 2025 (180-day eval): VSG is a desktop app, users are on Win10/11.
2. **Two image layers, mirroring the Dockerfile's `server` stage:**
   - `vsg-win11-os.qcow2`: Windows + OpenSSH + .NET 10 SDK + auto-logon + runner task + Defender exclusions.
     Slow (~30-60 min, unattended). Rebuilt only on `--rebuild-image`, or when older than the eval window.
   - `vsg-win11-server.qcow2` (overlay on `os`): steamcmd + `C:\valheim\vanilla` + `C:\valheim\bepinex`.
     ~3 min. Rebuilt on `--update-server`, or automatically when the `os` layer is rebuilt (backing chain).
   - Run overlays (`vsg-it-<cfg>-<pid>.qcow2`) back onto `server`.
3. **Single source for mod pins: `tests/integration-image/server-configs.env`** (BepInEx pack URL/sha256, plugin
   URL/sha256/dir, expected plugin). `integration.sh` passes them to `docker build` as `--build-arg`; the Dockerfile
   keeps the `ARG`s without defaults; `integration-windows.sh` reads the same file for the server bake. Replaces the
   spike's grep of the Dockerfile.
4. **Separate script, not a flag on `integration.sh`.** The Windows tier needs a one-time ~6 GB ISO download and an
   hour-long image build, and the `libvirt` group; `integration.sh` stays the fast default. Both can run at once.
5. **OpenSSH from Windows' own optional feature** (`Add-WindowsCapability OpenSSH.Server~~~~0.0.1.0`), installed
   by the first-logon script from the answer-file CD. Microsoft's inbox build, no pin to maintain (every
   Win32-OpenSSH GitHub release is tagged "Preview"). Host key auth only (dedicated key `~/.ssh/vsg_win_vm`, chmod
   600). Key goes to `administrators_authorized_keys` (stock sshd_config for an admin user); the spike's
   `authorized_keys` workaround was specific to the vagrant box.
6. **Local account `vsg` (admin), random password generated at image build**, stored only in the image's
   Winlogon `DefaultPassword` (required for auto-logon) and a gitignored file next to the image metadata. The VMs
   sit on libvirt's NAT network, not reachable from the LAN.
7. **Windows Update off, Defender exclusions** (not disabled; Tamper Protection blocks that) for `C:\dotnet`,
   `C:\vsg`, `C:\valheim`, `C:\Users\vsg\.nuget`, `C:\Users\vsg\.cache`. Build-time scanning is the main Windows
   slowdown.
8. **Keep the session-0 negative control as a flag (`--control <cfg>`)** so the A/B can be re-proven after harness
   changes. Not part of the default run.

## Files

| Path | Purpose |
| --- | --- |
| `scripts/integration-windows.sh` | Host entry point (flags below). Bash, same structure/output style as `integration.sh`. |
| `tests/integration-vm/autounattend.xml` | Unattended Win11 install: TPM/Secure Boot check bypass (LabConfig), disk layout, locale, local `vsg` admin, skip OOBE, first-logon command. |
| `tests/integration-vm/firstlogon.ps1` | From the answer-file CD: enable the inbox OpenSSH server, firewall rule 22, authorize the host key. Nothing else. |
| `tests/integration-vm/bake-os.ps1` | Over SSH: permanent auto-logon, no lock screen/power timeouts, Windows Update off, Defender exclusions, .NET 10 SDK to `C:\dotnet`, `vsg-runner` task (`/it /rl highest`). |
| `tests/integration-vm/bake-server.ps1` | Over SSH: steamcmd, vanilla install + `buildid.txt`, bepinex copy + sha-verified pack/plugin, firewall per exe. (Spike `bake.ps1` second half.) |
| `tests/integration-vm/run.ps1` | In the VM (session 1): transcript, refuse session 0 unless `-AllowServicesSession`, set `VSG_IT_*`, `dotnet test ... -p:Integration=true --logger trx --blame-hang`, write `exit_code.txt`, kill leftover servers. (Spike `run.ps1`.) |
| `tests/integration-image/server-configs.env` | Shared mod pins (decision 3). |

ISO + answer CD + image metadata cache: `~/.cache/valheim-server-gui/windows/` (outside the repo). Volumes live in
libvirt pool `default` (`/var/lib/libvirt/images`), created via the libvirt API (no sudo).

## Script interface

```
scripts/integration-windows.sh [--rebuild-image] [--update-server] [--keep] [--control <cfg>] [config...]
```

- default: both configs in parallel.
- `--rebuild-image`: rebuild the `os` layer (and therefore `server`).
- `--update-server`: rebuild only the `server` layer (new Valheim build).
- `--keep`: leave the run VMs up after the suite (prints IPs + destroy command) for debugging.
- `--control <cfg>`: negative control, suite over SSH in session 0; reports PASS only if the lifecycle tests FAIL.
- Artifacts: `dist/integration-windows/<cfg>/` (run.log transcript, .trx, blame dumps, captured server logs).
- Exit non-zero on any failure; terse `[ OK ]/[FAIL]` summary like the other scripts.

## Run flow

1. `validate.sh --quick` on the host (same gate as `integration.sh`).
2. Preflight: `virsh -c qemu:///system` reachable, key present and mode 600, ISO present (else download).
3. Image freshness:
   - `os` missing, `--rebuild-image`, or older than 80 days (stamp in metadata) → build `os` (step 6).
   - `server` missing, older than `os`, or `--update-server` → build `server` (step 7).
   - Print the image age, eval days left, and the server build id.
4. Per config, in parallel: create overlay on `server`, `virt-install --import` (8 GB, 4 vCPU, q35, OVMF
   non-secboot, SATA disk, e1000e NIC, no TPM), wait for DHCP lease + SSH, push git-tracked source as a tar
   (**extract with `tar xzmf`**: kept mtimes older than previous outputs make MSBuild skip the recompile), write
   `config.txt` / `expected-plugin.txt`, assert `query user` shows `vsg` Active in session 1, `schtasks /run`, poll
   `exit_code.txt` every 15 s with a 1500 s backstop, copy `C:\vsg\out` back, destroy VM + overlay (trap on EXIT).
5. Summary + exit code.
6. Build `os` layer: create empty 128G qcow2; build answer CD (`xorriso`, already installed) with
   `autounattend.xml`, `firstlogon.ps1`, public key; `virt-install` with ISO + answer CD; wait for
   SSH (key login); run `bake-os.ps1`; reboot once and assert auto-logon (`query user` → session 1 Active);
   `Stop-Computer`; wait for `shut off` **before** `undefine` (undefining a running domain leaves it running as a
   transient domain, still writing the image); write the stamp.
7. Build `server` layer: overlay on `os`, boot, run `bake-server.ps1` with the pins, clean shutdown, same
   shut-off check.

Cleanup only touches domains/volumes this script named (`vsg-it-*`, `vsg-win-build-*`); never Nuffle's VMs.

## Gotchas to carry over (from the spike)

- Windows PowerShell 5.1 with `$ErrorActionPreference = 'Stop'` turns any native-command stderr into a terminating
  error. Set `Continue` around `dotnet`/`steamcmd`, check `$LASTEXITCODE`.
- steamcmd's first run updates itself and can fail with "Missing configuration"; retry until the exe exists.
- New files under `~/.ssh` get mode 0640 from the directory's default ACL; ssh then ignores the key. `chmod 600`.
- Don't use `pgrep -f` in a wait loop on this host (safe-proc wrapper makes it match itself); wait on PIDs.
- Stale DHCP leases: always resolve the IP via `domifaddr --source lease` for the *current* domain name.
- `virsh screenshot <dom> x.ppm` shows the VM desktop for debugging a stuck install.

## Verification (before calling it done)

1. `--rebuild-image` from a clean cache: unattended install completes with no console interaction; record time.
2. Default run: vanilla and bepinex both pass (Core, App, live 6/6); record wall time.
3. `--control vanilla`: lifecycle tests fail (A/B proof that session 1 is what makes it pass).
4. Reboot check from step 6 passes; additionally reboot a run VM 3 times, session 1 each time.
5. `--update-server` rebuilds only the server layer (os volume untouched, mtime/hash unchanged).
6. Linux `integration.sh` still green after the pins move to `server-configs.env`; `validate.sh` clean.
7. Delete the spike volumes (`vsg-win11-base.qcow2`, `vsg-win11-golden.qcow2`) once 1-6 pass.

## Docs to update with the change

- `CLAUDE.local.md`: new "Windows integration (VM)" section (script, flags, image cadence, gotchas); the laptop
  section becomes the manual fallback.
- `docs/V3_TARGET_STATE.md`: live-smoke paragraph mentions the Windows tier.
- Project memory `windows-testing`: replace the spike paragraph with a pointer to this plan / the script.

## Open risks

- **ISO download URL:** pinned to a direct `software-static.download.prss.microsoft.com` URL + sha256 in
  `tests/integration-vm/image.env` (the eval center page itself is behind a bot wall). Microsoft refreshes the eval
  ISO a few times a year and old URLs may disappear. Fallback: download it by hand to
  `~/.cache/valheim-server-gui/windows/win11-eval.iso`; the script uses that file if it matches the pin.
- **Unattended Win11 on q35/OVMF without TPM:** relies on the LabConfig bypass keys in the windowsPE pass. If setup
  still refuses, switch the build VM to `--tpm emulator` + secboot OVMF (swtpm is installed); run VMs stay as-is.
- **Eval expiry:** an image older than 90 days shuts down hourly. The 80-day automatic rebuild prevents it; the
  rebuild also refreshes the OS and the Valheim build.
- **CI:** out of scope. A host-mode runner on a KVM host could call the script later.
