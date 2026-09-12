# ValheimServerGUI v3.0 — Target-State Specification

Status: draft for review. Branch: `v3.0`. Authoritative behavioral source: the v2.4
WinForms implementation (commit `cb48b90`), surveyed file-by-file. Every requirement below is
either a **parity requirement** (reproduce observed v2.4 behavior) or a **cross-platform
requirement** (replace a Windows coupling with an OS-agnostic design) or a **hardening
requirement** (fix a real defect the port should not carry forward). New capabilities are
explicitly fenced into §17 (Roadmap) and are **out of scope** for v3.0.

This document is the basis for the automated test suite. Sections 5–12 define behavior;
§13 defines the test strategy; §14 is the consolidated edge-case → test matrix; §15 lists
latent bugs to fix; §16 records settled decisions + remaining recommended defaults.

---

## 0. Purpose & scope

**Goal.** A clean-room rewrite of ValheimServerGUI as a cross-platform **desktop** application
(Windows + Linux), preserving the full v2.4 feature set and — critically — the same *structural
UI and user interactions* (not pixel-identical). The rewrite lifts and shifts the existing
service layer, replaces the WinForms shell with an Avalonia MVVM shell, and abstracts the small
number of Windows couplings behind per-OS implementations.

**In scope for v3.0:**
1. 1:1 parity of every v2.4 feature and interaction (see §5–§11).
2. Cross-platform operation on Windows and Linux **desktop** environments (see §12).
3. Necessary hardening: atomic config writes, corrupt-file backup, null-guards, and the latent
   bug fixes in §15.

**Out of scope for v3.0 (see §17 roadmap):** multi-signal monitoring fusion; the two-tier Server
Admin panel (live ban/whitelist/admin file editing; companion BepInEx mod); built-in scheduled
restart; in-app self-update; exposing the log-filter toggle; backup browse/restore; name
re-resolution TTL.

**Platform framing — desktop only.** v3.0 targets Windows and Linux **desktop** users running a
Valheim dedicated server on their own machine. It is NOT a headless/server-appliance tool. This
matters: a Linux desktop user has a real Steam client with `userdata` (so Steam Cloud world
import applies), a system tray (so minimize-to-tray applies), and a login session (so
start-on-login applies). SteamCMD / headless install flows are explicitly not a concern.

**Terminology (disambiguate in all UI/docs).** v2.4 used "cross-platform support" to mean
*player platforms* (Steam + Xbox crossplay). v3.0 uses "cross-platform" to mean *the app runs on
Windows and Linux*. In this spec and the product UI: **"crossplay"** = the Steam/Xbox player
feature; **"cross-platform"** = the app's OS support.

**Branding & backend.** Keep the Runeberry identity/branding and the existing Runeberry backend
(crash-report + player-name lookup Lambda) unchanged (see §8).

---

## 1. Target architecture

### 1.1 Solution layout (mirrors `../mochi-paint`)

```
src/
  Valheim.Core/            netX.0 (NO -windows). Lifted Game/ + Tools/ service layer, de-Windowsed.
                           All process/world/player/prefs/logging/update logic. No UI, no Avalonia.
  ValheimServerGUI.App/    Avalonia + CommunityToolkit.Mvvm. Thin shell: Views + ViewModels + DI
                           composition root + per-OS abstraction implementations.
tests/
  Valheim.Core.Tests/      The bulk of the safety net (xunit.v3). Pure logic, headless on Linux.
  ValheimServerGUI.App.Tests/  Avalonia.Headless.XUnit UI tests.
ValheimServerGUI.Serverless/       Kept as-is (ASP.NET Lambda; already cross-platform). See §8.
ValheimServerGUI.Serverless.Tests/ Kept as-is.
```

The v2.4 `ValheimServerGUI.Controls` project (custom WinForms controls) does **not** port —
Avalonia built-ins + styles replace it (see §11). The v2.4 `ValheimServerGUI.Tools` project
folds into `Valheim.Core`.

### 1.2 Stack pins (confirmed against mochi-paint)

- `net10.0`; `Nullable enable`; compiled bindings by default.
- Avalonia `12.1.0` (+ `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`).
- `CommunityToolkit.Mvvm` `8.4.0` (MVVM; observable properties + relay commands).
- `Microsoft.Extensions.Logging` `10.x` + `Microsoft.Extensions.DependencyInjection`.
- Serilog (logging pipeline lifts as-is; see §9).
- `xunit.v3` `3.2.2` + `Avalonia.Headless.XUnit` `12.1.0` + `Avalonia.Skia` (pixel-capable
  headless UI tests). Test projects are `OutputType=Exe`.
- Newtonsoft.Json (preserve the existing JSON contracts) — or migrate to System.Text.Json only
  if every existing on-disk contract round-trips (see §4.5); default: keep Newtonsoft to minimize
  migration risk.
- `Semver` (version comparison), `Humanizer` (relative time), `DeviceId` (crash-report machine
  fingerprint) — all cross-platform, keep.

### 1.3 Build & dev conventions

- **`Directory.Build.props` MUST redirect build output per-user** (`UseArtifactsOutput=true`;
  `ArtifactsPath=$(HOME)/.cache/valheim-server-gui/artifacts`). Reason: this repo is a shared
  `developers`-group checkout (users `mochi` + `nuffle`); in-tree `obj/` output triggers MSBuild
  `MSB3374` ("Access to the path …Up2Date is denied") for the second user because the up-to-date
  marker needs file ownership, not just group-write. This replaces the current
  `EnableWindowsTargeting` prop (which exists only to build WinForms on Linux and is obsolete once
  the shell is Avalonia).
- Indentation: spaces. Prefer built-in frameworks and fewer dependencies. DI over global state /
  monkey-patching (already the v2.4 style — preserve).

### 1.4 Per-OS abstraction seams (composition-root-selected)

The rewrite's entire Windows-coupling surface is small and localizes to these interfaces. Each
gets a Windows impl and a Linux impl selected in DI by `OperatingSystem.IsWindows()`.

| Seam | Windows impl | Linux impl | Replaces (v2.4) |
|---|---|---|---|
| `IAppPaths` (config/data/log dirs) | `%LOCALAPPDATA%`/LocalLow (see §4.4) | `$XDG_CONFIG_HOME`/`$XDG_DATA_HOME`/`$XDG_STATE_HOME` (else `~/.config`, `~/.local/share`, `~/.local/state`) | `Resources.resx` `%USERPROFILE%\AppData\LocalLow\...` literals |
| `ISteamPathResolver` | registry `HKCU\Software\Valve\Steam\SteamPath` | `~/.local/share/Steam`, `~/.steam/steam` (+ symlink), `~/.steam/root`, Flatpak `~/.var/app/com.valvesoftware.Steam/data/Steam` | `SteamCloudWorldProvider.GetSteamPath` (registry-only) |
| `IStartupManager` (start-on-login) | HKCU Run key (drop the HKLM tier — see §16) | XDG autostart `.desktop` in `~/.config/autostart/` | `StartupHelper` (registry) |
| `IShellLauncher` (open folder / URL) | `explorer.exe` or `ProcessStartInfo{UseShellExecute=true}` | `xdg-open` (fallback `gio open`); validate http/https + real local paths | `OpenHelper` (`explorer.exe` hardcoded) |
| Graceful server stop | send console-ctrl / graceful signal (never force-kill) | `kill(pid, SIGINT)` (never SIGKILL) | `ProcessExtensions.SafelyKillProcess` (`taskkill` no `/f`) |
| Default server binary | `valheim_server.exe` under `%ProgramFiles(x86)%\Steam\...` | `valheim_server.x86_64` under `~/.steam/steam/steamapps/common/Valheim dedicated server/` | `.exe`-hardcoded path validation |
| Default Valheim save dir | `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim` | `~/.config/unity3d/IronGate/Valheim` | `Resources.DefaultValheimSaveFolder` |

Everything else in the service layer is already OS-agnostic .NET (process spawn, `System.Net`,
Serilog, HTTP, semver) and lifts as-is.

### 1.5 Packaging & distribution

- **Windows:** single-file self-contained `.exe` (preserve the "just one small .exe" value prop).
- **Linux:** AppImage and/or Flatpak (desktop-integrated install), plus a self-contained tarball
  baseline with a `.desktop` launcher (mirror mochi-paint's `packaging/*.desktop.in`).
- **Update-notify assets:** the GitHub-releases update check keys on "a release with ≥1 asset"
  (§9.4). The release must carry per-OS assets; asset naming changes from the v2.4 `.zip`-with-
  `.exe` convention to include the Linux artifacts. The autostart `Exec`/relaunch command
  (§12) must record the correct invocation for the packaging format shipped
  (`Environment.ProcessPath`; AppImage/Flatpak launch differs from a bare apphost).

---

## 2. Application lifetime & shell

### 2.1 Splash-as-root → Avalonia lifetime (parity)

v2.4's application root is **`SplashForm`, not MainWindow**: it runs startup tasks, spawns one or
more `MainWindow`s, hides itself, stays alive to own process lifetime and global exception
handling, and closes (exiting the app) only when the **last** MainWindow closes.

**Target:** Avalonia `IClassicDesktopStyleApplicationLifetime` with
`ShutdownMode = OnLastWindowClose`, plus a startup service that:
1. Runs a visible splash window/view with a progress bar.
2. Runs async startup tasks (update check, player-data load) off the UI thread, updating progress.
3. On completion, opens the appropriate MainWindow(s) (§2.3), minimized if `StartMinimized`.
4. Hides/closes the splash.

Global exception coverage (hardening — §9.6): wire `AppDomain.CurrentDomain.UnhandledException`,
`TaskScheduler.UnobservedTaskException`, and Avalonia's dispatcher-unhandled hook (the WinForms
message loop implicitly caught these; Avalonia does not).

### 2.2 Multi-window (first-class parity requirement)

Multi-server support is realized as **multiple MainWindow instances**, one per open profile.
"File > New Window" opens another; auto-start profiles each open their own window at launch.
Windows share singleton providers (prefs, player repo, app log, IP, update) but each owns a
**transient `ValheimServer`** and a per-window server-log stream. The MVVM/DI scoping must
preserve this: **per-window** = server controller + server-log view-model; **shared singleton** =
player repo, app log, preferences, IP, updates.

**Decided: single-instance, multi-window.** A second launch focuses/adds to the existing instance;
multiple concurrent app processes are not supported (avoids the `userprefs.json` last-writer-wins
clobber; the v2.4 `FileSystemWatcher` reload path is dead code).

### 2.3 Startup profile selection (parity — `SplashForm.PrepareMainWindows`)

In order:
1. If any profile has `AutoStart == true` → open one window per auto-start profile and start each
   server.
2. Else → open the single most-recently-saved profile (by `LastSaved`), not started.
3. Else (no profiles) → create + save a `"Default"` profile and open it.

Per-window startup: load `StartProfile` into the form; if flagged auto-start, call the start flow
(non-manual variant); run the server-binary path existence check (§2.5).

### 2.4 Close / shutdown behavior (parity — "Safe shutdowns")

`Window.Closing` handler (Avalonia `e.Cancel` + async deferred close):
- **User-close while Starting/Running:** confirm dialog ("server still running — stop and
  close?"). Yes → graceful `Stop()` + defer the close until the server reports `Stopped`, then
  close. Cancel the immediate close regardless.
- **User-close while Stopping:** confirm ("shutting down — close anyway? may lose save data").
  No → cancel; Yes → allow close.
- **Server already Stopped:** close proceeds.
- **OS shutdown/logoff:** graceful `Stop()` + wait-for-stopped, cancel immediate close so the
  save flushes. (Windows: `WindowsShutDown` reason; Linux: session-end signal — map to the
  Avalonia lifetime shutdown-requested hook.)
- App exits when the last window closes.

### 2.5 First-run / setup (parity)

There is no EULA/accepted-terms gate; "first run" is inferred from absence of the config file.
On window show, validate the configured server binary exists; if missing, prompt "File Not
Found" → offer to open the Directories dialog. This is the closest thing to a setup prompt and
must be preserved.

---

## 3. Server profiles & configuration

### 3.1 Two persistence stacks (parity, then unify — see §16)

1. **Preferences stack** — a single `userprefs.json` aggregate holding app-level settings + a
   `servers` list (multi-server profiles) + a `worlds` list (world-gen configs). One file owner.
2. **Data-repository stack** — a generic keyed JSON store (`DataFileRepository<TEntity>` +
   `IPrimaryKeyEntity`), used only for `players-cache.json` (§7).

Every domain model splits into a **runtime model** (non-nullable, baked-in defaults) and a
nullable **`*File` DTO** with explicit JSON property names. `FromFile`/`ToFile` null-coalescing is
the sole defaulting mechanism, making missing keys fall back to defaults (not `0`/`false`/`null`).
**This two-model pattern is load-bearing for forward/backward-compatible config evolution and
must be preserved and tested.**

### 3.2 Server profile model (`ServerPreferences`) — enables multi-server

One instance = one named profile, keyed by `ProfileName`. Fields (JSON key / default):

| Field | JSON | Default | Notes |
|---|---|---|---|
| `ProfileName` | `profileName` | `"Default"` | logical primary key |
| `LastSaved` | `lastSaved` | `UnixEpoch` | stamped `UtcNow` on save; ordering + duplicate tiebreak |
| `Name` | `name` | `null` | in-game server name (≠ ProfileName) |
| `Password` | `password` | `null` | plaintext by design — the server password is a shared join passphrase freely given to players, not a user credential; no encryption/perms/warning |
| `WorldName` | `world` | `null` | world to host; created if absent |
| `Public` | `community` | `false` | note key is `community` |
| `Port` | `port` | `2456` | |
| `Crossplay` | `crossplay` | `false` | |
| `SaveInterval` | `saveInterval` | `1800` (s) | |
| `BackupCount` | `backupCount` | `4` | |
| `BackupIntervalShort` | `backupIntervalShort` | `7200` (s) | |
| `BackupIntervalLong` | `backupIntervalLong` | `43200` (s) | |
| `AutoStart` | `autoStart` | `false` | start this profile at app launch |
| `AdditionalArgs` | `additionalArgs` | `null` | appended verbatim to launch |
| `ServerExePath` | `valheimServerPath` | `null` | per-profile override of global; blank → global |
| `SaveDataFolderPath` | `valheimSaveDataFolder` | `null` | per-profile override of global; blank → global |
| `WriteServerLogsToFile` | `writeServerLogsToFile` | `true` | |

A profile references a world only by `WorldName` (string); world-gen settings live separately in
`WorldPreferences` (§6.5) and are merged at launch. Two profiles hosting the same world share one
world-gen config (intentional).

### 3.3 App-level preferences (`UserPreferences`)

| Field | JSON | Default | Notes |
|---|---|---|---|
| `ServerExePath` | `valheimServerPath` | per-OS default binary | global server binary |
| `SaveDataFolderPath` | `valheimSaveDataFolder` | per-OS default save dir | global save folder |
| `CheckForUpdates` | `checkForUpdates` | `true` | gates automatic update checks |
| `StartWithWindows` → rename `StartOnLogin` | `startWithWindows` (keep key for migration) | `false` | see §12 autostart |
| `StartMinimized` | `startMinimized` | `false` | minimize to tray on launch |
| `SaveProfileOnStart` | `saveProfileOnStart` | `true` | auto-persist form on server start |
| `WriteApplicationLogsToFile` | `writeApplicationLogsToFile` | `true` | |
| `EnablePasswordValidation` | `enablePasswordValidation` | `true` | feeds launch validation |
| `Servers` | `servers` | `[]` | profile collection |
| `Worlds` | `worlds` | `[]` | world-gen collection |

**Deprecated keys the deserializer MUST tolerate (ignore) on load** for in-place upgrades:
`startServerAutomatically`, `checkServerRunning`, `valheimGamePath`.

**Not modeled today (decisions in §16):** theme/appearance, log verbosity (only file-write
booleans exist), update-check interval (hardcoded 24h), a persisted "last active profile", and
`LogFilteringDisabled` (a real behavior with no UI).

### 3.4 Multi-server operations (parity — File menu)

- **New Profile:** prompt name (1–30 chars, unique) → save blank profile (Name defaults to
  profile name) → load into form.
- **Save / Save As:** Save writes form → active profile; Save As prompts a name (default "Copy of
  {current}"), clones, switches active profile to the clone.
- **Load Profile ▸:** submenu of all profiles ordered by `LastSaved` desc; click loads into form.
  **Disabled while a server is running** (profile switching blocked mid-run).
- **Remove Profile ▸:** submenu; click confirms Yes/No then removes.
- Active profile is a per-window runtime concept (raises `ProfileChanged` → updates window title,
  tray tooltip, tray header). **Not persisted** today (see §16 for optional "last active").
- Upsert-by-name semantics: saving removes all entries with the same `ProfileName`, re-adds,
  stamps `LastSaved`. Duplicate names on disk are de-duped on load/save; a named lookup that still
  finds duplicates returns the most-recently-saved and logs a warning.

### 3.5 Persistence mechanism (parity + hardening)

- Serializer: Newtonsoft, indented. Per-instance `ReaderWriterLockSlim` guards load/save.
- Corrupt/missing file → deserialization failure is caught + logged, returns defaults; app never
  fails to start on bad config.
- **HARDENING (required):**
  - **Atomic writes** — write to a temp file then atomic-rename. v2.4 truncates in place
    (`File.CreateText`), so a crash mid-write corrupts `userprefs.json`.
  - **Corrupt-file backup** — before overwriting a corrupt config with defaults, copy it aside
    (`.corrupt-<ts>`) and surface a warning. v2.4 silently overwrites → data loss.
  - Guard `WorldPreferences.FromFile` against a null `keys` JSON key (currently NPEs).
  - Guard `AdditionalArgs.ToLower()` against null in validation.

### 3.6 Directories configuration (parity)

The Directories dialog edits the two **global** default paths (`ServerExePath`,
`SaveDataFolderPath`); per-profile overrides live on the profile and win when non-blank. On save,
paths are validated by constructing options and calling the path validators; failure shows
"Save anyway?" (Yes proceeds, No reverts). Invalid paths CAN be saved deliberately. The file
picker's `.exe` filter and default root must become platform-aware (§12).

### 3.7 Launch-time validation (parity — `ValheimServerOptions.Validate`)

The authoritative validation, run when starting a server (target: move into the start flow so
every entry point is protected — §5.2):
- `Name` required; `WorldName` required; `Name != WorldName`.
- Password (only when `EnablePasswordValidation`): public server must have a password; non-blank
  password ≥ 5 chars; must not contain the server name or the world name.
- Port in `1..65535`.
- `SaveInterval ≥ 1`, `BackupShort ≥ 1`, `BackupLong ≥ 1`; `SaveInterval ≤ BackupShort`;
  `SaveInterval ≤ BackupLong`; `BackupShort ≤ BackupLong`.
- World-gen: if a preset is set it must be a known preset and modifiers must be empty (mutual
  exclusion); else each modifier key/value must be in the allowed sets; each key must be known.
- `AdditionalArgs` must not contain `-logfile` (case-insensitive) — it would divert stdout away
  from the app's capture and break every log-based feature.
- Server binary + save dir validated last (existence).

Target improvement: centralize validation so a profile can be validated at edit/save time with
field-level errors (Avalonia `INotifyDataErrorInfo` / `DataValidationErrors`), not only at launch.

### 3.8 Migration (parity)

Legacy `userprefs.txt` (`key=value`) → `userprefs.json`: parse recognized keys
(`ValheimServerPath` → user path; `ServerName/Password/WorldName/Public/Port` → a synthesized
single profile), save JSON, delete the `.txt`. Unparseable values fall back to defaults; migration
failure falls back to defaults. **Decision (§16):** keep this migration or drop it (the format
predates multi-server).

---

## 4. File locations & config paths (cross-platform)

All config/data/log paths come from `IAppPaths` (§1.4), never from string literals with Windows
env vars.

- **Config dir** (`userprefs.json`, `players-cache.json`): Windows keep LocalLow
  `…\Runeberry\ValheimServerGUI\` (migration continuity + preserves the "Steam can't overwrite it"
  property — config lives in a vendor dir, never in the game install or the game's save folder);
  Linux `$XDG_CONFIG_HOME/ValheimServerGUI` else `~/.config/ValheimServerGUI`. (Decision to move
  Windows to `%APPDATA%` deferred — §16; recommend keep LocalLow.)
- **Log dir**: Windows LocalLow `…\logs`; Linux `$XDG_STATE_HOME/ValheimServerGUI/logs` else
  `~/.local/state/ValheimServerGUI/logs`.
- **"Open settings directory"** menu opens the config dir via `IShellLauncher`.
- Env-var expansion for unexpanded literals must be defined (an unexpanded `%USERPROFILE%` must
  not be treated as a relative path).

The "It remembers! / can't be overwritten by Steam" value prop is an emergent property of storing
config in a per-user app dir separate from the game install and the game's save folder — preserve
it on both OSes.

---

## 5. Server lifecycle & process management

### 5.1 State machine (parity)

Four states: `Stopped` (0), `Starting` (1), `Running` (2), `Stopping` (3). No error/crashed state
today (adding one is roadmap — §17). Transitions:

- `Stopped → Starting`: `Start(options)` (guard `CanStart`). Spawns process, wires stdio + Exited,
  sets status.
- `Starting → Running`: log line **`Game server connected`** matched (unless currently Stopping).
- `Starting/Running → Stopping`: `Stop()` / `Restart()` (guards `CanStop`/`CanRestart`).
- `* → Stopped`: child process `Exited` fires (the ONLY liveness signal — no polling/A2S/RCON).
- `Stopped → Starting` (auto-restart): only via `Restart()`, using a 500ms-delayed re-`Start`
  gated on an `IsRestarting` flag re-checked after the delay.

Capability gates (tray + buttons both derive from these — do not duplicate):
`CanStart = Stopped && no process`; `CanStop = (Starting|Running) && process`;
`CanRestart = Running && process`. `Start/Stop/Restart` are silent no-ops if their guard fails.

Invariants to pin: (a) a stop request during `Starting` is never overridden by a late
`Game server connected` line (Stopping wins); (b) a `Stop()`/`Start()` during the 500ms restart
window cancels the pending restart.

### 5.2 Start flow (parity)

1. (UI) If an existing selected world ends with the `" (cloud)"` suffix → cloud-import prompt
   (§6.4) before proceeding.
2. Build `ValheimServerOptions` from form state.
3. `Validate()` (target: owned by the start flow, not the caller — §3.7). Manual start → error
   dialog; auto start → log only.
4. Port availability: both `port` and `port+1` must be free (Valheim uses two consecutive UDP
   ports). Warn if in use.
5. New-world validation (name 5–20 chars, must not already exist) or existing-world existence.
6. Spawn the child process with `SteamAppId=892970` env var (required for Steam auth; same on both
   OSes), redirected stdout/stderr, `UseShellExecute=false`, raising-events on. Set status
   `Starting`.
7. If `SaveProfileOnStart` (default true) → save the profile.

### 5.3 GenerateArgs — the server-launch contract (parity)

Base template (always): `-nographics -batchmode -name "{Name}" -port {Port} -world "{WorldName}"
-public {0|1} -savedir "{saveDir}" -saveinterval {SaveInterval} -backups {Backups} -backupshort
{BackupShort} -backuplong {BackupLong}`.

Conditional:
- `-password "…"` only if non-blank (scrubbed to `*****` in logs).
- `-crossplay` only if true.
- `-preset {v}` if a preset is set — **mutually exclusive** with modifiers (preset wins; modifiers
  skipped).
- `-modifier {key} {value}` once per modifier, only when no preset.
- `-setkey {key}` once per key (independent of preset).
- `{AdditionalArgs}` appended verbatim if non-blank.

The arg set was validated against the Valheim Dedicated Server manual and is unchanged by the
2026 save-format update — the v2.4 breakage was detection-only, not command-building. Linux uses
the same args; only the binary name/path differ (§12).

### 5.4 Graceful stop (cross-platform — the key portability fix)

v2.4 shells out to Windows `taskkill /pid <id>` **without `/f`** so the server runs its shutdown
handler and flushes the world save (a hard `Process.Kill()` risks save corruption). This is the
single most important portability fix.

**Target:** a `IProcessProvider.StopGracefully(key)` abstraction:
- Windows: send a graceful termination (console-ctrl event, or keep a `taskkill`-no-`/f`-style
  helper) — never force-kill.
- Linux: `kill(pid, SIGINT)` (Valheim's Linux server installs a signal handler that saves + shuts
  down cleanly) — never `SIGKILL`.

`Stop()` signals then returns; the `Stopped` transition happens asynchronously on `Exited`. Kill
of an already-exited process is a safe no-op. **Hardening (§16):** add a stop-timeout →
escalation (with explicit save-loss warning) so a signal that never takes effect doesn't strand
the server in `Stopping`.

### 5.5 Log-based event detection (parity — brittle; pin verbatim; make config-driven)

Every stdout/stderr line is matched (case-insensitive) against all patterns; capture groups pass
to the handler; handler exceptions are caught, never fatal. Only pattern #1 drives server state.
There is NO pattern for crash / world-gen failure / port-in-use (silent gaps — roadmap §17).

| # | Regex (verbatim) | Event |
|---|---|---|
| 1 | `Game server connected` | → `Running` (unless Stopping) |
| 2a | `World saved \(\s*?([[\d\.]+?)\s*?ms\s*?\)\s*?$` | legacy (pre-1.0) world-save duration → `WorldSaved` |
| 2b | `World save \(\d+/\d+\) done\. Total time \[([\d.]+)ms\]` | **Valheim 1.0+** world-save total time → `WorldSaved` (the live smoke found the pre-1.0 line is gone; pattern 2a alone silently stopped detecting saves) |
| 3 | `Session ".*?" with join code (.*?) ` | crossplay invite code → `InviteCodeReady` |
| 4 | `Got connection SteamID (\d+?)\D*?$` | Steam player joining |
| 5 | `PlayFab socket with remote ID .*? received local Platform ID (\w+?)_(\d+?)$` | crossplay player joining (platform_id) |
| 6 | `Got character ZDOID from (.+?) : ([\d-]+?)\D*?:(\d+?)\D*?$` | player online (character name + ZDOID; **ZDOID may be negative**) |
| 7 | `Peer (\d+?) has wrong password` | player rejected (→ Leaving) |
| 8 | `Closing socket (\d+?)\D*?$` | disconnect (best-effort terminator) → Offline |
| 9 | `Destroying abandoned non persistent zdo ([\d-]+?):.*$` | crossplay disconnect → Offline |
| 10 | `Disconnect: The client \((\w+?)_(\d+?)\)` | Valheim-Plus version-mismatch disconnect → Offline |

These patterns depend on exact Valheim log strings; a game update that rewords any silently breaks
the corresponding feature with no error — as happened to the world-save line (2a → 2b), caught by the
Phase 2.5 tier-4 live smoke. **Target:** externalize the pattern → handler table as configuration so a
Valheim update can be patched without a code release, and treat it as a known fragility point in tests
(golden capture-once/replay — §13, plus the live-log fixture-drift guard in the tier-4 smoke).

Events emitted by the server controller: `StatusChanged`, `WorldSaved`, `InviteCodeReady`. Player
events flow into the player repository, not through these events.

### 5.6 Edge cases — see §14 test matrix (exe missing, save-dir missing, world-gen failure,
port-in-use, unexpected death, close-while-running, multiple windows, double-start, late-stop
race, taskkill/signal-missing, stuck-Stopping).

---

## 6. World management

### 6.1 Detection (parity)

Scan both `<savedir>/worlds` and `<savedir>/worlds_local` (non-existent folder skipped silently),
merged as one set. Two formats:
- **Legacy:** loose `<WorldName>.fwl` (+ `.db`). Identity = filename.
- **New (Valheim 1.0+):** a folder `<WorldName>/` containing ≥1 `*.fwl2` (+ `_main.N.db2`,
  `.chunks`, `.ok`, region `.chunk`). Identity = **folder name** (the data file is `_main.N`, not
  the world name; `.db2` is gzip; world-version int is 41 vs legacy 34).

**A directory is a world iff it contains ≥1 `*.fwl2`.** This single predicate must back
enumeration, availability, AND cloud-source resolution (v2.4 has a gap — see §15).

### 6.2 Backup exclusion (parity)

Exclude names matching `^.*?_backup_(auto-\d|\d+?-\d+?)` — covers both `_backup_<date>-<time>` and
`_backup_auto-<ts>`. Apply to both legacy file names and folder names.

### 6.3 World creation / selection UI (parity)

Two radio modes: **Existing** (read-only dropdown, empty text "(no worlds)"; enabled only if ≥1
world) and **New** (text field, MaxLength 20). Toggling swaps values. When a "new world" server
reaches Running, flip to Existing and select the just-created world. New-world validation on start:
non-empty, **5–20 chars**, must not already exist. **Decided (§16):** names are case-insensitive on
both OSes (`MyWorld` == `myworld`) and filesystem-illegal characters are sanitized on input so
folder creation is always valid on Linux. (v2.4 had only length + MaxLength; a sanitizer helper
existed but was unused on the creation path.)

### 6.4 Steam Cloud world import (parity, cross-platform — desktop Steam client present)

In-game worlds save to Steam Cloud (`userdata/<id>/892970/remote/worlds/<World>/`); the dedicated
server never reads them. The provider surfaces them (via `ISteamPathResolver` — §1.4) and
copies/moves them into the local savedir. Flow:
- Enumerate cloud worlds (the `remote` folder is a drop-in savedir; reuse the same enumeration),
  dedup case-insensitively.
- Merge into the dropdown with a UI-only `" (cloud)"` suffix; local wins on name collision.
- On start of a `(cloud)`-suffixed world: 3-button dialog **Move / Copy / Cancel** (Copy default).
  Import copies the world folder into `worlds_local/<name>` (recursive, overwrite); Move then
  deletes the source (a locked source with Steam running is logged-and-treated-as-success since the
  copy is usable). Availability guard: block only if a **real** local world exists — a cache-only
  folder (`cacheMinimap*`, no `.fwl2`) is importable into. Never leave a partial copy; never delete
  a pre-existing cache folder. Re-list without the suffix and re-select, then start.

Windows uses the registry Steam path; Linux uses the resolver's native/Flatpak candidates. Align
cloud enumeration with the `.fwl2` source predicate so a legacy-format cloud world isn't listed as
importable but then fail (v2.4 gap — §15).

### 6.5 World-gen options (parity)

Stored per-world in `WorldPreferences` (`WorldName`, `LastSaved`, `Preset`, `Modifiers` dict,
`Keys` set), keyed by world name; merged into launch options.

- **Presets** (`-preset`): `normal, casual, easy, hard, hardcore, immersive, hammer`. UI display
  order: Custom(no preset) / Easy / Normal / Hard / Hardcore / Casual / Hammer Mode / Immersive.
  Choosing a preset shows expanded modifiers/keys for display, but a preset world persists as
  `-preset` only (individual modifiers not saved). Editing any modifier/key reverts to Custom.
- **Modifiers** (`-modifier key value`, only when no preset): `combat`
  (veryeasy/easy/hard/veryhard), `deathpenalty` (casual/veryeasy/easy/hard/hardcore), `resources`
  (muchless/less/more/muchmore/most), `raids` (none/muchless/less/more/muchmore), `portals`
  (casual/hard/veryhard). "Normal" = empty value = omitted.
- **Keys** (`-setkey key`): `nobuildcost, playerevents, passivemobs, nomap, fire`. Emitted
  independently of preset. **Fix the v2.4 asymmetry** (§15): `GenerateArgs` emits keys always but
  the form only persists keys in the custom branch — unify so persisted state matches emitted args.

### 6.6 Backups (parity)

App does not create/manage backups (Valheim does via `-backups`/`-backupshort`/`-backuplong`);
the app only excludes backup files from the world list (§6.2). Defaults 4 / 7200 / 43200; save
interval 1800; interval-ordering validation per §3.7. No backup browse/restore UI (roadmap §17).

---

## 7. Player management

### 7.1 Model (parity — preserve JSON contract for in-place upgrade)

`PlayerInfo` (persisted to `players-cache.json`, keyed `"{Platform}:{PlayerId}"`):
- Persisted: `platform` (Steam|Xbox), `playerId`, `playerName` (nullable until resolved),
  `lastStatusChange`, `lastStatusCharacter`, `characters[]` (`characterName`, `matchConfident`).
- **Transient (JsonIgnore, reset on load):** `PlayerStatus` (Offline/Joining/Online/Leaving) and
  `ZdoId`. The cache is a historical roster; live status is ephemeral.

No cross-platform account linking (one real person = distinct record per platform).

### 7.2 Lifecycle & the correlation heuristic (parity — top unit-test target)

Join carries a platform ID; "online" carries only a character name + ZDOID (no platform ID) — so
the app must correlate. `SetPlayerOnline` resolution priority (each branch is a distinct test):
1. Records whose characters include the name: exactly one → confident (and revert a stray
   Joining record of the same key back to Offline, restoring its cached offline timestamp); >1 →
   the single Joining one if unique, else unresolved.
2. Else among all Joining: exactly one → confident; >1 → **best-guess earliest joiner, flagged
   `matchConfident=false`**; zero → unresolved.

Disconnect matches `PlayerId` OR `ZdoId` (Or-query). Every mutation upserts + saves immediately
(consider debouncing — perf, §16). **Hardening (§16):** on server stop/exit, explicitly reset all
live statuses to Offline rather than relying on disconnect log lines being emitted.

### 7.3 Name resolution (parity)

On join, if `PlayerName` is empty → fire-and-forget Runeberry `GET /player-info` (§8). One-shot
per unnamed player; resolved name persists permanently (no TTL — decision §16). Offline/failure
degrades silently (keeps the `[...NNNN]` fallback; retries on next join since name still empty).

### 7.4 Players tab + Player Details (parity)

- Table columns: **Player (Character)** (display name = `PlayerName` or `[...last4 of PlayerId]`,
  plus ` (character)` when known), **Status**, **Since** (relative time). Platform icon per row;
  offline rows greyed. Live-updated from repo `EntityUpdated`; "Since" recomputed every 1s while
  the tab is visible.
- Actions: **View Player Details** (enabled when a row is selected) and **Remove Player** (enabled
  only when the selected player is **Offline**). A "character names wrong?" help link.
- Player Details dialog: read-only identity fields (name, platform ID + copy, ZDOID, latest
  character, status, status-changed); editable display name (0–64 chars, manual override) and
  editable known-characters list (added names get `matchConfident=true`); unsaved-changes
  Yes/No/Cancel guard on close. Pulls fresh data from the repo by key.

### 7.5 Crossplay & invite code (parity)

Platforms are exactly Steam + Xbox (case-normalized; unknown → not recorded). The crossplay
invite/join code is **server session state** (not a player field): shows "Loading…" while a
crossplay server starts, populated from a log line, cleared to "N/A" on stop; copy button visible
only when a copyable code is present. Model it separately from player records.

---

## 8. Runeberry backend (keep as-is)

`ValheimServerGUI.Serverless` (ASP.NET on AWS Lambda) is already cross-platform and stays
unchanged. The Avalonia client must reproduce the two calls and their contracts:

- **`GET /player-info?platform=&playerId=`** → `{id,name,platform}`. Server resolves Steam
  (Steam Web API) / Xbox (OpenXBL) display names; 400 on missing/unsupported platform. Client
  calls it fire-and-forget from `SetPlayerJoining` when name unknown; ignores responses with any
  empty field.
- **`POST /crash-report`** ← `CrashReport` (`id, clientCorrelationId, source, timestamp,
  appVersion, osVersion, dotnetVersion, currentCulture, currentUiCulture, additionalInfo, logs`).
  Stored to S3. Client calls it consent-gated (crash) or user-initiated (bug report); `source`
  distinguishes the two.

Both require an API-key header from `ClientSecrets` (out-of-source-control partial class —
preserve the build/config-time secret-injection pattern; do not hardcode). All HTTP failures are
caught + logged + return null → the app is fully functional offline (only name enrichment and
report submission need connectivity).

**Privacy note (kept behavior):** the player-info call sends a platform ID (PII) to the Lambda for
every unknown joiner, with no opt-out. Kept as-is per decision; an opt-out is roadmap (§17).

---

## 9. Logging, updates, crash reporting, versioning

### 9.1 Logging (parity)

Two Serilog streams on a shared `BaseLogger`: **Application** (singleton, app diagnostics) and
**Server** (per-server-instance, fed by the child process stdout/stderr). Surfaced as two named UI
views. `BaseLogger` runs an ordered filter/transform rule chain (order matters; exclude
short-circuits at first `false`; transforms compose cumulatively), lazy-builds its Serilog logger,
and is rebuildable (Application log rebuilds on prefs-saved so "write logs to file" takes effect
live). Two consumers: the in-memory ring buffer + a `LogReceived` event the UI subscribes to.

- **Ring buffer:** fixed 1000-line FIFO (`ConcurrentBuffer`); backs the UI view (populate on
  switch/startup) and the **last-100-lines** slice in crash/bug reports. (Make size configurable —
  §16.)
- **"Cleaner server logs"** = three exclude filters on the server chain (`^(Filename:`,
  `^Console: `, blank lines) plus stripping Valheim's own timestamp and re-adding a clean one;
  gated by `LogFilteringDisabled` (always-on today; exposing it is roadmap §17).
- **File logging:** daily rolling, 30-day retention, shared-write; Application log file gated by
  `WriteApplicationLogsToFile`, per-profile server log file (`ServerLogs-{Name}`) gated by
  `WriteServerLogsToFile`. Log dir via `IAppPaths` (§4). Validate Serilog `shared:true` semantics
  on Linux.

### 9.2 Update checks (parity — pure logic already unit-tested; port verbatim)

Notify-only (no self-update — roadmap §17). `CheckForUpdatesAsync(isManual)`:
- Automatic checks throttled to once per `UpdateCheckInterval` (24h) and gated by `CheckForUpdates`;
  manual checks bypass both.
- Fetch the **full** GitHub releases list (`repos/runeberry/ValheimServerGUI`, `User-Agent`
  header). `SelectLatestRelease`: keep releases with ≥1 asset AND not draft AND not
  GitHub-pre-release-flagged; order by `PublishedAt` desc; first, else null.
- **Release-process invariant (hard requirement):** exclusion is by GitHub's *pre-release flag*,
  NOT the version string. A pre-release *version* like `2.4.0-rc.1` published as an ordinary
  release with an asset is deliberately eligible. RCs must ship as **non-pre-release releases that
  carry an asset** or users aren't notified. Keep the guarding tests.
- `CompareVersions` uses Semver precedence (`2.4.0` > `2.4.0-rc.1`; naive string compare is wrong):
  returns 1 (other newer) / -1 (older) / 0 (equal) / -2 (unparseable); accepts a leading `v`.
- UI: right status-bar item shows Checking / Update available (link) / Up to date / Pre-release
  build / failed (link); a manual check adds a Yes/No dialog offering to open the releases page
  (via `IShellLauncher`). A 60s timer re-runs the silent check.

### 9.3 Crash & bug reporting (parity)

- Global unhandled exceptions → opt-in Yes/No dialog → build report → `POST /crash-report` shown
  via an async-operation dialog. **Add proper global hooks (§9.6).**
- Bug report dialog: description (Submit enabled only when non-empty) + optional contact; same
  pipeline with `source="BugReport"` and `additionalInfo={BugReport,ContactInfo}`; both carry the
  last-100 log lines and the machine fingerprint (`DeviceId`: MD5 of MAC + machine name — verify on
  Linux; only stability matters).

### 9.4 Versioning (parity + hardening)

Informational version is `"<semver>+build<datetime>"`; the build pipeline must keep injecting the
`+build<datetime>` suffix (`SourceRevisionId`) or build-date parsing throws. **Harden**
`GetApplicationVersion` to not crash when `+build` is absent (v2.4 does `str[..IndexOf("+build")]`
unguarded — §15).

### 9.5 IP resolution (parity)

External IP via `api.ipify.org` (silent no-op on failure — consider a fallback chain, §16).
Internal IP via `System.Net.NetworkInformation` (cross-platform): up interfaces with gateways,
non-loopback IPv4, prefer DHCP-origin (validate on Linux — some stacks report Other/Unknown; the
fallback to "all eligible" is acceptable), alphabetical tiebreak. UDP port availability via active
listeners. Server Details shows External/Internal/Local (127.0.0.1), each with a copy button;
`:port` appended only when the port differs from the default.

### 9.6 Global exception coverage (hardening)

Wire `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, and
Avalonia's dispatcher-unhandled-exception hook. The WinForms message loop implicitly caught these;
Avalonia does not, and v2.4 only had an `Application.Run` try/catch + per-startup-task handling.

---

## 10. UI — Main window (the interaction bible)

Reproduce the same structure and interactions; not pixel-identical. All service events arrive
off-thread and must be marshalled to the UI thread (Avalonia `Dispatcher.UIThread`), or bound via
observable VM properties that the framework marshals.

### 10.1 Chrome & menus

- Small, resizable window (min ~500×371); **not maximizable** today (decision §16). Window/tray
  title derives from the active profile (title, tray tooltip, tray header all read one
  `CurrentProfile` source).
- **File menu:** New Window · New Profile (Stopped-only) · Save Profile · Save Profile As… · Load
  Profile ▸ (has-profiles + Stopped-only) · Remove Profile ▸ (has-profiles) · Preferences… · Set
  Directories… · Open Settings Directory… · Close. Preserve keyboard mnemonics.
- **Help menu:** Online Manual · Port Forwarding · Submit a Bug Report… · Check for Updates ·
  Get support in Discord · About…. (External links via `IShellLauncher`.)

### 10.2 Tabs (fixed order, 5 tabs)

Fields are editable only while `Status==Stopped` (a single "server changes allowed" gate). Each
field is a labeled input with optional tooltip/help.

- **Server Controls:** Server Name (max 64); Port (1–65535); Password (masked) + Show-password
  toggle + Copy; World group (Existing/New radios, existing dropdown, new-name field max 20,
  world-settings gear → World Preferences dialog, refresh worlds, open save folder); Join Options
  (Community Server, Enable Crossplay); **Start / Restart / Stop** buttons.
- **Advanced Controls:** Directory Overrides (per-profile server exe [platform-aware picker] + save
  folder, each with open button); Other Settings (auto-start this server, write server logs to
  file, additional command-line args max 32767); Saving & Backups (save interval 60–86400, backups
  1–1000, short 300–2592000, long 300–2592000).
- **Server Details** (read-only, refreshed on tab-visible + 1s timer): Connection Details (External
  / Internal / Local IP each + copy; Invite Code + copy; port-forwarding hint); Statistics (Server
  Uptime, Last World Save `{time} ({ms})`, Avg World Save = rolling mean of last ≤10 saves).
- **Players:** the table + actions (§7.4).
- **Logs:** view selector (Server / Application, default Server) → LogViewer; Clear Logs (current
  view only); Save Logs… (current view to `.txt`; warn if empty); open logs folder.

### 10.3 Start/Stop/Restart & status bar

Three always-visible buttons, each enabled by `CanStart`/`CanRestart`/`CanStop`. Tray Start/Stop/
Restart items mirror the same predicates (derive, don't duplicate). Status bar left = server
status (enum name + icon); right = update-check status, clickable as a link when an update/failure
state is a link. `StartServer(isManualStart)` distinguishes button/tray starts (error dialogs)
from auto-start (log only).

### 10.4 System tray (parity — Linux tray required per decision)

Tray icon + tooltip (profile-derived) + context menu (profile header → focus window; separator;
Start/Restart/Stop mirroring button enablement; separator; Close). Left-click restores + activates
the window. Minimize hides from the taskbar (window lives in the tray). No balloon/toast
notifications today (adding them is a decision, not parity). Avalonia `TrayIcon` + `NativeMenu`;
provide a windowed fallback where a Linux DE lacks tray support so the app never becomes
unreachable when minimized.

### 10.5 Observable surface for MVVM

The VM(s) expose: `ServerStatus` (+ derived `CanStart/Stop/Restart`, `AllowServerChanges`,
`StatusText/Icon`), uptime, invite-code state, `LastWorldSave`/`AverageWorldSave`, an
`ObservableCollection` of player rows, update-status (text/icon/is-link + link command), IP labels,
active-profile, and the two log views. Tab-activation drives lazy refresh (bind to tab
`IsSelected`).

---

## 11. UI — Secondary dialogs & control library

### 11.1 Dialogs (parity)

- **About:** logo, credits, disclaimer, version + build date, buttons (GitHub / Donate / Valheim
  site / Discord — all open URLs).
- **Preferences:** six booleans (auto-save-on-start, check-for-updates [help text interpolates the
  interval], start-on-login, start-minimized, write-app-logs-to-file, validate-password). OK saves
  (start-on-login applies via `IStartupManager`); Cancel discards; Restore Defaults repopulates
  fields only (no save until OK). Load in an on-open hook so a reused dialog shows current values.
- **Directories:** global server-binary path (platform-aware picker) + save folder + open buttons;
  OK validates-by-construction with a "Save anyway?" override; Restore Defaults repopulates only.
- **Bug Report:** description + optional contact; Submit gated on non-empty; submits via the crash
  pipeline in an async-operation dialog; fields cleared on open + close.
- **Player Details:** §7.4.
- **World Preferences:** the world-gen preset/modifier/key editor (§6.5).
- **Text-prompt dialog:** generic single-line input with a validation predicate → returns
  `string?` (null on cancel). Used for profile names, player-name edit, character add/edit.
- **Async-operation dialog:** runs a `Task` behind a modal with in-progress + success/failure
  states. Used for report submission.

Dialogs are modal `Window` + VM; `ShowDialog<T>` is async — blocking `Prompt`/`ShowDialog`
callers become async, including the editable-list add/edit delegates (→ `Func<Task<string?>>`).

### 11.2 Control-library → Avalonia mapping

The v2.4 `IFormField<T>` kit (label + input + help glyph; idempotent setters that only raise
`ValueChanged` on real change) maps to styled Avalonia built-ins wrapped in a shared "form row"
template. Preserve the behaviors, not the WinForms internals:

| v2.4 control | Behavior to reproduce | Avalonia replacement |
|---|---|---|
| TextFormField | text, password-mask, max length, multiline | `TextBox` (PasswordChar/RevealPassword, MaxLength, AcceptsReturn) |
| NumericFormField | int, clamp to min/max | `NumericUpDown` |
| Checkbox/RadioFormField | bool; radio grouped by name | `CheckBox` / `RadioButton` (GroupName — native mutual exclusion; prefer binding one enum/selected-value) |
| DropdownFormField | single-select over strings, empty sentinel → null | `ComboBox` (ItemsSource/SelectedItem; model "none" explicitly) |
| LabelField | read-only label+value row, split ratio | two-`TextBlock` grid row |
| FilenameFormField | text + file/folder picker, filters | `TextBox` + `Button` → `StorageProvider.Open*PickerAsync`; drop `.exe`-only / ProgramFilesX86 root on Linux |
| SelectListField | single-select listbox + add/remove API | `ListBox` + `ObservableCollection` |
| DataListView | sortable, entity-bound, multi-column grid (players) | `DataGrid` (sortable headers, selection); bind columns to VM props / converters (e.g. relative time) rather than delegate extractors |
| LogViewer | multi-view buffered read-only log | read-only text control fed per-view `ObservableCollection`; consider virtualization for large logs |
| HelpLabel | "?" glyph with tooltip, hidden when empty | icon + `ToolTip.Tip`, collapse when empty |
| IconButton (+ Copy/Edit/Open/Refresh/Settings) | icon button with success "confirm flash"; `Func<bool>` success gate | one command-bound icon `Button` + a shared confirm-flash style; distinct icon/tooltip per use |
| AddRemoveListField | editable string list (add/edit/remove); character list, args-like lists | `ListBox` + Add/Edit/Remove commands; button enablement bound to selection + per-action flags |

Reusable patterns to carry: OK/Cancel/Restore-Defaults dialog shape (load on open, flush on OK);
unsaved-changes guard; copy-to-clipboard (`TopLevel.Clipboard`); open-folder / open-URL
(`IShellLauncher`); generic text-prompt; generic async-operation dialog. Prefer
`INotifyDataErrorInfo`/`DataValidationErrors` for inline validation (an upgrade over v2.4's
MessageBox-on-invalid). Log suppressed exceptions rather than swallowing silently (keep the
don't-crash-the-UI intent).

---

## 12. Cross-platform behavior matrix

| Feature | Windows | Linux (desktop) |
|---|---|---|
| Server binary (default) | `valheim_server.exe` under `%ProgramFiles(x86)%\Steam\…` | `valheim_server.x86_64` under `~/.steam/steam/steamapps/common/Valheim dedicated server/` |
| Binary picker filter | executables; default `.exe` | no `.exe` requirement; validate "is an executable file" |
| Graceful stop | console-ctrl / taskkill-no-`/f` (never force) | `SIGINT` (never `SIGKILL`) |
| Config/data dir | LocalLow `…\Runeberry\ValheimServerGUI\` | `$XDG_CONFIG_HOME`/`~/.config/ValheimServerGUI` |
| Log dir | LocalLow `…\logs` | `$XDG_STATE_HOME`/`~/.local/state/…/logs` |
| Default save dir | `…\AppData\LocalLow\IronGate\Valheim` | `~/.config/unity3d/IronGate/Valheim` |
| Steam path (cloud import) | registry `HKCU\Software\Valve\Steam\SteamPath` | `~/.local/share/Steam`, `~/.steam/steam` (+symlink), `~/.steam/root`, Flatpak path |
| Start-on-login | HKCU Run key (drop HKLM tier) | XDG autostart `.desktop` in `~/.config/autostart/` (Exec = correct relaunch for the shipped package) |
| Minimize to tray | NotifyIcon | Avalonia `TrayIcon` + windowed fallback where DE lacks tray |
| Open folder / URL | `explorer.exe` / shell-execute | `xdg-open` (fallback `gio open`); validate http/https + real paths |
| Steam auth env | `SteamAppId=892970` | same |
| Server CLI args | §5.3 | identical |
| Packaging | single-file `.exe` | AppImage / Flatpak (+ tarball baseline) |

All three OS-specific features the user flagged (autostart, tray, Steam Cloud import) have real
Linux implementations — this is a desktop app on both platforms, never headless.

---

## 13. Testing strategy

MVVM enforces the UI/logic split; the safety net lives mostly in `Valheim.Core.Tests`. Four tiers:

1. **Core / ViewModel unit tests** (xunit.v3, headless on Linux). Inject fully mocked providers
   (process, HTTP, file, prefs) — no real filesystem, no real Windows paths (fix the v2.4 test that
   fakes a running server with real Windows paths). Cover: GenerateArgs arg emission, state-machine
   transitions + guards + late-stop race + restart window, validation rules + boundaries,
   world detection matrix, world-gen arg mapping, the player join→online correlation heuristic
   (every branch), name-resolution offline degradation, config defaulting/round-trip/migration,
   update selection + version comparison, log rule chain, ring-buffer eviction, IP selection,
   crash-report building, per-OS path/steam/startup resolvers (inject the OS boundary), the §15 bug
   fixes.
2. **Log-parser golden tests (capture-once / replay-forever).** Capture real dedicated-server
   stdout once → immutable fixtures → replay through the parser. Pins the fragile regex table
   (§5.5) as a snapshot; a Valheim update that changes strings surfaces as a fixture/behavior
   diff, not silent breakage. Fixtures cover: connect, world-save, invite code, Steam + crossplay
   join/leave, negative ZDOID, wrong-password, version-mismatch disconnect, and a legacy-world
   migration sequence.
3. **Avalonia headless UI tests** (`Avalonia.Headless.XUnit`). Cover the high-value stateful UI
   flows: button/tray enablement tracks status; tab-visible refresh; new-world post-start
   reselection; cloud-world Move/Copy/Cancel; profile new/save/save-as/load/remove; close-while-
   running defer; dialogs' OK/Cancel/Restore-Defaults + unsaved-changes guard; log view switching.
4. **Live smoke** (no game client) — **automated** (`scripts/integration.sh` +
   `tests/ValheimServerGUI.Integration.Tests`, Phase 2.5). Boots the **real Linux dedicated server**
   (Steam appid 896660) with `-public 1` and asserts lifecycle (Starting→Running via the real
   "connected" line), the graceful-stop save-flush A/B that `taskkill`/SIGINT is there to guarantee
   (graceful SIGINT advances the world-save mtime; a hard kill does not), a Steam Cloud world
   import round-trip, and a fixture-drift guard (the captured live log still matches the parser
   patterns). The test project is intentionally **not** in `ValheimServerGUI.slnx` and every test is
   env-gated (machine-specific Steam paths from a gitignored config), so `validate.sh` never runs it
   and an accidental direct run skips. Tiers 1+4 cover each other's blind spots (fixture snapshot vs.
   current binary). No headless *client* exists anywhere, so join/leave-with-names stays fixture-based
   (tier 2).

Design principles enforced in tests (per project conventions): **derive-never-mirror** (tray and
buttons read one `Can*` accessor; title/tooltip/header read one `CurrentProfile`); **pin
capability, not user-authored data** (world-detection fixtures the code controls, not counts of
real save files); **reproduce the reported state before claiming a fix, then A/B** (graceful-stop
save-flush must be shown to fail under force-kill).

---

## 14. Consolidated edge-case → test matrix

| # | Area | Condition | Required behavior | Tier |
|---|---|---|---|---|
| E1 | Lifecycle | Server binary missing/bad path | Start throws before ProcessKey set; state stays Stopped; UI surfaces the error | 1 |
| E2 | Lifecycle | Save dir missing | Same as E1 (throws in GenerateArgs) | 1 |
| E3 | Lifecycle | World-gen fails / world load error | (v2.4 gap) surfaced, not a silent hang — add a startup timeout/pattern (roadmap; test the timeout hook) | 1/2 |
| E4 | Lifecycle | Port (or port+1) in use | Pre-start check warns; start blocked/flagged | 1 |
| E5 | Lifecycle | Process dies unexpectedly | Exited → Stopped, no auto-restart; UI shows Stopped | 1 |
| E6 | Lifecycle | Stop during Starting; late "connected" arrives | Stopping wins; never promoted to Running | 1 |
| E7 | Lifecycle | Restart window interrupted by Stop/Start | Pending 500ms restart cancelled | 1 |
| E8 | Lifecycle | Close while Running / Stopping / OS shutdown | Confirm + graceful stop + deferred close (§2.4) | 3 |
| E9 | Lifecycle | Graceful stop | SIGINT/console-ctrl flushes a save; force-kill would not (A/B) | 4 |
| E10 | Lifecycle | Stop signal never takes effect | Stop-timeout escalation with save-loss warning (hardening) | 1 |
| E11 | World | Cache-only folder (`cacheMinimap*`, no `.fwl2`) | Not a world; still "available" (importable over) | 1 |
| E12 | World | Backup names (both schemes) | Excluded from listing | 1 |
| E13 | World | Same world in `worlds` + `worlds_local` | Dedup (fix v2.4 double-listing) | 1 |
| E14 | World | Legacy `.fwl` + new folder mixed | Both listed uniformly | 1 |
| E15 | World | Nested `.fwl2` (`W/sub/x.fwl2`) | Not counted (non-recursive) | 1 |
| E16 | World | New-world name <5 or >20 chars / already exists | Rejected with message | 1 |
| E17 | World | Legacy-format cloud world | Enumeration + source predicate aligned (fix v2.4 mismatch) | 1 |
| E18 | World | Cloud import Move with Steam-locked source | Copy succeeds; failed delete = warning, treated as success | 1 |
| E19 | World | Cloud import partial-copy failure | Delete dest only if we created it; never destroy pre-existing cache | 1 |
| E20 | World | Case sensitivity `MyWorld` vs `myworld` | Same world on both OSes (case-insensitive); illegal chars sanitized on input | 1 |
| E21 | Player | Unknown platform in log line | Not recorded | 1/2 |
| E22 | Player | Name resolution offline/failure | Silent; `[...NNNN]` fallback; retries next join | 1 |
| E23 | Player | Name response missing fields | Ignored | 1 |
| E24 | Player | Multiple simultaneous joiners, no name match | Best-guess earliest joiner, `matchConfident=false` | 1 |
| E25 | Player | Misattributed character then corrected | Stray Joining reverted to Offline, original timestamp restored | 1 |
| E26 | Player | Rejoin after offline | Reuses most-recently-offline record (no dup) | 1 |
| E27 | Player | Negative ZDOID | Matched by `[\d-]+` | 1/2 |
| E28 | Player | Disconnect by ID vs ZDOID | Or-query resolves either | 1 |
| E29 | Player | Remove while online | Blocked (offline-only) | 3 |
| E30 | Player | App restart | Roster persists; status/ZDOID reset to Offline/null | 1 |
| E31 | Player | Server stop | All live statuses reset to Offline (hardening) | 1 |
| E32 | Config | Missing config (first run) | Defaults; Default profile created + saved | 1 |
| E33 | Config | Corrupt config | Backed up aside, then defaults (hardening; not silent overwrite) | 1 |
| E34 | Config | Deprecated keys present | Ignored, no failure | 1 |
| E35 | Config | Legacy `.txt` present | Migrated to JSON, `.txt` deleted (if migration kept) | 1 |
| E36 | Config | Missing JSON key | Runtime default via nullable `*File` coalesce | 1 |
| E37 | Config | Crash mid-write | Atomic rename prevents corruption (hardening) | 1 |
| E38 | Config | Duplicate profile/world names | De-duped; named lookup returns most-recent + warns | 1 |
| E39 | Config | Blank profile/world name | Dropped on save | 1 |
| E40 | Config | `WorldPreferences` null `keys` | Guarded (no NPE) | 1 |
| E41 | Update | Pre-release *version* as ordinary release + asset | Eligible (RC notified) | 1 |
| E42 | Update | Draft / asset-less releases | Skipped | 1 |
| E43 | Update | No qualifying release / GitHub unreachable | Treated as up-to-date (no error spam) | 1 |
| E44 | Update | RC user vs final release | Final seen as update (semver, not string compare) | 1 |
| E45 | Update | Unparseable version | `-2` sentinel → "unable to parse" state | 1 |
| E46 | Version | Informational version missing `+build` | No crash (hardening) | 1 |
| E47 | Logging | Ring buffer overflow | Oldest evicted, count capped, thread-safe | 1 |
| E48 | Logging | Server chain filtering on/off | Unity-noise + blank + native-timestamp handling | 1/2 |
| E49 | Logging | LogLevel transformer | Information has no tag; others tagged | 1 |
| E50 | Logging | Server-name with path-illegal chars | File name sanitized | 1 |
| E51 | IP | Internal IP: DHCP preference, loopback exclusion, empty→warning | Selection rules (Linux DHCP-origin fallback ok) | 1 |
| E52 | IP | External IP blank/failure | No-op, previous value kept | 1 |
| E53 | Startup | Autostart enable/disable/stale-path/idempotent | Windows registry & Linux `.desktop` both (inject OS boundary) | 1 |
| E54 | Shell | Open folder/URL, invalid path, non-http scheme | Correct per-OS launcher; scheme/path validated | 1 |
| E55 | Controls | AddRemoveListField Edit/Remove enable flags | Each returns its own flag (fix v2.4 bug) | 1 |
| E56 | Time | `TimeAgo.Equals` / format string | Fixed (no throw; format honored or replaced by converter) | 1 |

---

## 15. Latent bugs to fix (do NOT port verbatim)

1. `AddRemoveListField.EditEnabled` / `RemoveEnabled` getters both return `_isAddEnabled` instead
   of their own backing fields.
2. `TimeAgo.Equals(TimeAgo)` throws `NotImplementedException`; `ToString(format, provider)` ignores
   the format string. (Recommend replacing `TimeAgo` with a VM converter over `DateTimeOffset`.)
3. `AssemblyHelper.GetApplicationVersion` crashes when the `+build` suffix is absent (unguarded
   `IndexOf`).
4. `WorldExists` (backs `IsWorldNameAvailable`) does not apply the backup-exclusion regex —
   enumeration and availability disagree; route both through one predicate.
5. World-gen keys asymmetry: `GenerateArgs` emits `-setkey` always, but the form only persists keys
   in the custom (non-preset) branch — unify.
6. `GetWorldNames` does not dedup a world present in both `worlds` and `worlds_local`.
7. `WorldPreferences.FromFile` NPEs on a null `keys` JSON key.
8. Config writes are non-atomic (in-place truncate) and corrupt files are silently overwritten with
   defaults — add atomic-rename + corrupt-file backup.
9. No global unhandled-exception hooks beyond the WinForms message loop — add them (§9.6).
10. Steam Cloud enumeration lists legacy-format worlds that `GetCloudWorldFolder` can't resolve
    (would throw on import) — align the predicates.
11. `discordlogo.png` resx casing (a v2.4 latent Linux build break) — moot in a clean-room rewrite
    but note the class of bug: verify asset casing matches on-disk on Linux.

---

## 16. Decisions

### 16.1 Settled (this session)

- **JSON serializer:** keep **Newtonsoft.Json** — round-trips every existing on-disk contract
  unchanged (nullable `*File` defaulting, deprecated-key tolerance, dictionary/set fields). No
  migration risk.
- **Concurrency:** **single-instance app**, multi-window. A second launch focuses the existing
  instance; concurrent app processes are not supported (avoids `userprefs.json` clobber).
- **Password storage:** **plaintext**, no file-perm hardening, no warning. The server password is a
  shared join passphrase freely given to players, not a user credential — keep the port simple.
- **World/profile names:** **case-insensitive on both OSes** (`MyWorld` == `myworld`, matching
  Windows today, no surprise Linux duplicates) and **filesystem-illegal characters sanitized on
  input** so folder creation is always valid on Linux. (v2.4 had only the 5–20 length rule.)

### 16.2 Remaining recommended defaults (confirm or override)

1. **Windows config dir:** recommend **keep LocalLow** (migration continuity + "Steam can't
   overwrite" property) rather than moving to `%APPDATA%`.
2. **Startup scope:** recommend **per-user only** on both OSes (drop the Windows HKLM "all users"
   tier — it needed admin and silently fell back to HKCU anyway).
3. **External-IP resiliency:** recommend a small **fallback chain** (ipify → ifconfig.co →
   icanhazip) so one outage doesn't blank the field.
4. **Legacy `userprefs.txt` migration:** recommend **keep** (cheap, one-time).
5. **Theme:** recommend **add a light/dark/system preference** (Avalonia norm; small).
6. **Persist last-active profile:** recommend **yes** — add `LastActiveProfile` so restart reopens
   it (small UX win).
7. **Stop-timeout escalation:** recommend **add** a graceful-stop timeout → force-kill fallback
   (with explicit save-loss warning) so a signal that never lands doesn't strand the server in
   `Stopping`.
8. **Window maximizable / minimize-on-close toggle:** v2.4 is non-maximizable and always
   stops-on-close. Recommend **keep as-is** unless you want to relax.
9. **Log-buffer size:** hardcoded 1000 — make configurable? Low priority, recommend leave.

---

## 17. Post-v3.0 roadmap (explicitly OUT of scope)

Documented so the parity spec stays clean; each is researched in prior sessions.

- **Multi-signal monitoring fusion** — derive server state from process liveness + A2S_INFO poll
  (count when not crossplay; `-crossplay` pins A2S count to 0) + log parse, mod-optional. Adds a
  richer, less log-fragile status. (Research: prior "monitoring signals" investigation.)
- **Two-tier Server Admin panel** — Tier A: live-edit `adminlist`/`bannedlist`/`permittedlist`
  `.txt` files, picked up by a stock server within ~15s (SyncedList 10s reload + 5s enforce),
  no client/mod/restart; keyed by platform ID (which the player model already holds). Tier B: full
  devcommand surface via an optional companion BepInEx mod bridging stdin → `Console.TryRunCommand`
  (no game client), depending on JereKuusela Server-devcommands for headless enablement (check its
  license before bundling). (Research: prior "server control channels" investigation.) Greenfield —
  no v2.4 behavior exists.
- **Built-in scheduled restart** — replace the wiki's Windows batch + Task Scheduler + forced
  `taskkill` recipe (which skips the autosave) with an in-app timer doing a **graceful** save-then-
  stop-then-start, cross-platform. Fixes the documented data-loss caveat.
- **Crashed/faulted server state + supervised auto-restart-on-crash** (with backoff/cap) +
  startup-failure detection (world-gen fail, port-in-use patterns / timeout).
- **In-app self-update** (per-OS packaging complexity) — keep notify-only for v3.0.
- **Expose the `LogFilteringDisabled` toggle** in the UI (a real behavior with no control today).
- **Backup browse/restore UI.**
- **Player-name re-resolution TTL / manual re-fetch** (names are cached forever today).
- **Player-ID lookup opt-out / privacy notice** (PII leaves the machine per join today).
- **Tray balloon/toast notifications** (none today).

---

## Appendix A — Source-of-truth map (v2.4 → v3.0)

| Concern | v2.4 source | v3.0 target |
|---|---|---|
| Server process/state | `Game/ValheimServer.cs`, `ValheimServerOptions.cs`, `Tools/Processes/*` | `Valheim.Core` server controller + `IProcessProvider` (cross-platform stop) |
| World detection/import | `Game/ValheimPathExtensions.cs`, `SteamCloudWorldProvider.cs`, `WorldGen*.cs` | `Valheim.Core` world services + `ISteamPathResolver` |
| Players/backend | `Game/PlayerDataRepository.cs`, `PlayerInfo.cs`, `Tools/RuneberryApiClient.cs`, `Serverless/*` | `Valheim.Core` player repo + kept Serverless |
| Prefs/config | `Game/*Preferences*.cs`, `Tools/Data/*` | `Valheim.Core` prefs + `IAppPaths` + atomic file provider |
| Logging/update/crash/IP/version | `Tools/Logging/*`, `SoftwareUpdateProvider.cs`, `GitHubClient.cs`, `ExceptionHandler.cs`, `IpAddressProvider.cs`, `AssemblyHelper.cs` | `Valheim.Core` (mostly verbatim) + `IShellLauncher`/`IStartupManager` |
| Main window | `Forms/MainWindow.cs` (1528L) + `.Designer.cs` | `ValheimServerGUI.App` MainWindow view + view-model(s) |
| Secondary dialogs | `Forms/*Form.cs`, `Forms/*Popout.cs` | `ValheimServerGUI.App` dialog views + VMs + dialog service |
| Controls | `ValheimServerGUI.Controls/*`, `Controls/*` | Avalonia built-ins + shared styles/templates |
