# Plan: explicit mod support (launch controls + BepInEx log)

Status: planned (2026-10-16). Ships in 3.0.0-rc.2; rc.2 is not cut until this is done.

## Goal

Linux BepInEx loading (ea9061b) currently rests on one hard-coded assumption: the pack sits in the server
exe's folder. Give users a visible default plus an override for every assumption VSG makes when launching a
modded server, keep the UI compact, and make Windows mod output visible in VSG.

Non-goals: a mods on/off switch (no demand; no known off switch on Windows; Linux users can point the paths
elsewhere), mod installation/listing/updating (mod managers' job), a key-value editor UI, running user
launch scripts (would break graceful stop: the signal reaches the shell, not the server).

## Background: what VSG sets today when launching the server

| Variable | When | How |
| --- | --- | --- |
| `SteamAppId` | always | `892970` |
| `LD_LIBRARY_PATH` | Linux | prepends `<exeDir>:<exeDir>/linux64` to the inherited value |
| `DOORSTOP_ENABLED` | Linux, pack in exe dir | `1` |
| `DOORSTOP_TARGET_ASSEMBLY` | Linux, pack in exe dir | `<exeDir>/BepInEx/core/BepInEx.Preloader.dll` |
| `LD_PRELOAD` | Linux, pack in exe dir | prepends `<exeDir>/doorstop_libs/libdoorstop_x64.so` to the inherited value |

Windows: Doorstop is `winhttp.dll` + `doorstop_config.ini` in the server folder; the game loads it itself.
VSG sets nothing for it, matching v2. That does not change.

## Decisions

1. **Mods load on Linux iff both configured files exist.** Two new paths: BepInEx preloader (.dll) and Doorstop
   library (.so). Both exist → set the Doorstop variables (table above, using the configured paths). Otherwise
   → vanilla, and the app log says which file was missing. Windows ignores both paths (unchanged behavior).
2. **The two paths are ordinary directory settings,** same rules as the server binary and save data folder: an
   app-wide value with an OS-derived default, plus an optional per-profile override. Defaults derive from the
   resolver's default server folder, not from the configured server binary (Nuffle: simple fields beat clever
   ones; a user who moves the server binary moves these too):
   - preloader: `<default server dir>/BepInEx/core/BepInEx.Preloader.dll`
   - doorstop: `<default server dir>/doorstop_libs/libdoorstop_x64.so`
3. **Profile environment variables: one text field,** "Additional Environment Variables", under "Additional
   Command Line Args". Syntax in decision 6. Parsed values go into the server process environment.
4. **Environment layering:** inherited env → profile variables (replace same-named inherited ones) → VSG's
   launch requirements on top. For `LD_LIBRARY_PATH` / `LD_PRELOAD` the profile value is the base and VSG
   prepends its entries (as it already does for inherited values).
5. **Disallowed variables, checked at Start** (same place as the `-logfile` check, `ValheimServerOptions`
   validation, because files can change between save and start):
   - `SteamAppId`: always.
   - `DOORSTOP_ENABLED`, `DOORSTOP_TARGET_ASSEMBLY`: only when VSG is loading BepInEx itself (Linux, both files
     exist). Otherwise they pass through, so a hand-rolled Doorstop setup still works.
   - Each rejection names the variable and the reason (resx message per case).
6. **Environment variable syntax** (unit-tested, one parser in Core):
   - Whitespace-separated `NAME=VALUE` assignments. `NAME` matches `[A-Za-z_][A-Za-z0-9_]*`.
   - `VALUE` is unquoted (runs to the next whitespace, taken literally: backslashes are kept, so `C:\x` works),
     single-quoted (literal, no escapes), or double-quoted (`\"` and `\\` are the only escapes). Quoted and
     unquoted parts may be adjacent (`A=pre"fix x"`), like a shell.
   - Empty value allowed (`A=` / `A=""`). Duplicate name: last one wins.
   - Malformed input (no `=`, bad name, unterminated quote) fails Start validation with a message naming the
     problem token.
   - No `$VAR` expansion (the value reaches the process exactly as written).
7. **The "Server run command" log line includes the variables as typed:** `Server run command: <env field text>
   "<exe>" <args>` (password masking unchanged). This is what makes the tooltip's "prepended" literally true.
8. **Directories move off the Advanced Controls tab into the Directories dialog** (details in UI below).
9. **BepInEx log in the Server log view, Windows only.** On Windows BepInEx's console logger writes to `CONOUT$`
   (hidden by `CreateNoWindow`) and never reaches stdout; on Linux plugin lines already arrive via stdout.
   VSG tails `<exeDir>/BepInEx/LogOutput.log` for this boot and feeds its lines through the same path as
   stdout lines (Server view, server log file, parser).

## UI

### Advanced Controls tab (`Views/Tabs/AdvancedControlsView.axaml`)

- Remove the "Directory Overrides (for just this Profile)" group box (`AdvancedControls_DirectoryOverrides_Header`,
  `AdvancedControls_ServerExe_Help`, `AdvancedControls_SaveData_Help` go away).
- New top row, same pattern as the Players tab's "Manage Players" row: a "Set Directories…" button that opens the
  Directories dialog with the current profile selected.
- Under "Additional Command Line Args": `c:TextFormField` "Additional Environment Variables",
  `MaxLength="32767"`, `IsEnabled="{Binding AllowServerChanges}"`. Help text (exact, Nuffle): "Set additional
  environment variables here. These will be prepended to the beginning of the command generated by
  ValheimServerGUI."

### Directories dialog (`Views/Dialogs/DirectoriesWindow.axaml`, `DirectoriesViewModel`)

- Replace the intro `TextBlock` (`Directories_Intro`) with `c:DropdownFormField` "Set directories for:" listing
  "All profiles", a separator, then every profile name.
  - File > Set Directories opens it on "All profiles"; the Advanced tab button opens it on the current profile.
- Fields, in order:
  1. "Valheim Dedicated Server binary" (renamed from "… .exe"; `Paths_ServerExe_Label`).
  2. "Valheim Save Data Folder".
  3. "BepInEx Preloader .dll" (Linux only). Help (exact, Nuffle): "The location of BepInEx.Preloader.dll.
     This library is required when loading mods on Linux."
  4. "Doorstop Library .so" (Linux only). Help: "The location of libdoorstop_x64.so. This library is required
     when loading mods on Linux."
- One help text per field regardless of selection. Server binary help's first line becomes (exact, Nuffle):
  "The install location of the Valheim Dedicated Server application." (the NOTE paragraph stays).
- **"All profiles" selected:** fields hold the app-wide values (defaults filled in, as today).
- **A profile selected:** fields hold that profile's overrides; an empty field shows the placeholder
  "(use default)" and means "use the app-wide value". Read-only while that profile's server is running.
- **Switching the selection keeps edits:** pending edits are held per selection; OK saves every changed target,
  Cancel discards all. No prompts.
- Restore Defaults: "All profiles" → OS defaults (as today); a profile → clears its overrides.
- Open-folder buttons open the effective path (override, else app-wide value).
- "Save anyway?" missing-path check covers all four fields and every changed target.

### Controls needed

- `FilenameFormField` gets a `Watermark` property (for "(use default)"); check how `TextFormField` does it and
  reuse that.
- `DropdownFormField` gets separator support (no separator support exists today). Keep the separator out of the
  selectable values.

## Core changes

- `UserPreferences` / `UserPreferencesFile`: `BepInExPreloaderPath`, `DoorstopLibraryPath`.
  `UserPreferencesProvider.ApplyPathDefaults` fills them from new `IValheimPathResolver` members
  (`DefaultBepInExPreloaderPath`, `DefaultDoorstopLibraryPath`, derived from `DefaultServerPath`'s folder).
- `ServerPreferences` / `ServerPreferencesFile`: the same two as nullable overrides, plus `EnvironmentVariables`
  (raw string).
- `ValheimServerOptions` / `IValheimServerOptions`: `BepInExPreloaderPath`, `DoorstopLibraryPath`,
  `EnvironmentVariables` (raw); validation per decisions 5-6.
- New `EnvironmentVariableParser` (static, pure) → ordered list of (name, value) or a parse error.
- `ValheimServer.StartCore`: apply profile vars, then existing Linux composition; `EnableBepInExLoader` takes
  the two configured paths instead of deriving from `exeDir`; log "loading BepInEx from <dll>" or "BepInEx not
  loaded: <path> not found" (application log, never localized).
- Windows log tail: a small `BepInExLogTail` started by `ValheimServer` on Windows after process start, stopped
  on exit.
  - Waits for `<exeDir>/BepInEx/LogOutput.log` with mtime ≥ process start; polls (~500 ms), opens with
    `FileShare.ReadWrite | FileShare.Delete`, reads appended complete lines, resets on truncation.
  - Lines go to the same handler as stdout lines.
  - Skip lines from the `Unity Log` source (BepInEx's `[Logging.Disk] WriteUnityLog`, off by default, would
    otherwise duplicate stdout lines and feed status patterns twice).
  - Missing file (vanilla, or the disk log disabled in BepInEx config) → nothing, no error.
  - Thin file I/O wrapper so the unit tests drive it with a fake.

## App changes

- `ServerFormViewModel`: drop `ServerExePath` / `SaveDataFolderPath`; add `EnvironmentVariables`.
- **Profile save must not wipe dialog-owned fields.** `MainWindowViewModel.BuildPreferences()` builds the whole
  `ServerPreferences` from the form; the four path overrides now come from the stored profile at save time.
  Same for Save Profile As (copy the source profile's overrides). Unit test: set an override via the dialog,
  edit + save the form, override survives.
- `BuildOptions()`: resolve all four paths as override ?? app-wide value; pass `EnvironmentVariables`.
- `DirectoriesViewModel`: selection list, per-target pending edits, save to user prefs or each profile's prefs.
- Remove `OpenServerExeFolderCommand` / `OpenSaveFolderCommand` from the main VM if nothing else uses them.

## Strings (`Strings.resx`, then `./scripts/gen-test-strings.sh`)

- Changed (exact, Nuffle): `Paths_ServerExe_Label` = "Valheim Dedicated Server binary"; first line of
  `Directories_ServerExe_Help` = "The install location of the Valheim Dedicated Server application."
- New (exact, Nuffle): Additional Environment Variables label + help; "BepInEx Preloader .dll" label + help;
  "Set directories for:"; "All profiles"; "(use default)".
- New (mirrors Nuffle's wording, approved): "Doorstop Library .so" label + help.
- New, copy for Nuffle to review in the diff: "Set Directories…" button; validation messages (disallowed variable
  ×2 reasons, malformed variable syntax); missing-path prompts for the two new fields.
- Removed: `AdvancedControls_DirectoryOverrides_Header`, `AdvancedControls_ServerExe_Help`,
  `AdvancedControls_SaveData_Help`, `Directories_Intro`.

## Tests

Unit (Core.Tests / App.Tests):
- Parser: every syntax rule in decision 6, incl. Windows paths, adjacent quoting, empty values, duplicates, each
  malformed case.
- Validation: `SteamAppId` always rejected; `DOORSTOP_*` rejected only when both files exist on Linux, accepted
  otherwise and on Windows.
- Launch env (`MockProcessProvider`): profile vars set; `LD_PRELOAD` / `LD_LIBRARY_PATH` keep the user value with
  VSG's entries in front; BepInEx enabled only when both files exist; custom paths used verbatim.
- Run-command log line includes the env text and still masks the password.
- Log tail: waits for a fresh file, ignores a stale one, appends, handles truncation, drops `Unity Log` lines.
- Prefs: defaults derive from the resolver; overrides fall back; form save keeps dialog overrides.
- Directories VM: per-selection pending edits, OK saves all targets, Cancel none, Restore Defaults per mode,
  read-only for a running profile.

Integration (both `integration.sh` and `integration-windows.sh`):
- `bepinex`: the fixture passes the two paths explicitly. Mod-loader test asserts `Loading [<plugin>]` and
  `Chainloader startup complete` in the **captured server log** on both OSes (Windows now gets them via the
  tail), replacing the direct `LogOutput.log` read.
- New config `bepinex-external` (Linux): pack + plugin installed outside the server folder, paths pointed at
  it → plugin loads. Verify BepInEx resolves its root from the preloader location first.
- A/B before calling it done: Windows red with the tail disabled; Linux red with the paths pointed at a
  missing file; `bepinex-external` red with default paths.

## Docs to update with the change

- `docs/V3_TARGET_STATE.md`: Advanced tab, Directories dialog, Logs (§10.x), mod loading.
- `CLAUDE.local.md` integration section (new config, mod-loader assertion), project memory `project-state`
  (deferred items resolved).
- `../vsg-docs`: Directories + Advanced Controls pages, a "Running a modded server" page (Linux paths, env var
  syntax). Copy for Nuffle.
- Pilot user note after rc.2: don't use `start_game_bepinex.sh`; install the pack into the server folder.

## Open risks

- **`bepinex-external` may not work as-is:** BepInEx derives paths from the preloader's location, but Doorstop
  or the pack may assume the game dir in places. The integration config settles it; if it fails, document
  "pack must be in the server folder" and drop the config, keeping the path fields as plain overrides.
- **Tailing while BepInEx writes:** BepInEx opens `LogOutput.log` for writing with sharing that should allow
  readers (the test fixture reads it today, but after boot). Verify concurrent reads early in the Windows VM.
- **Log ordering on Windows:** tailed lines arrive up to one poll later than stdout lines; acceptable.
