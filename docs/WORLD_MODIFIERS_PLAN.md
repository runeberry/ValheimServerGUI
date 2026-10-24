# World modifiers: fields as truth + `-resetmodifiers`

Status: IMPLEMENTED 2026-10-23 (steps 1-7); step 8 (docs-site FAQ) pending Nuffle. Decisions below. Game version analyzed: 1.0.20 (decompiled
`tmp/valheim-decompiled/`, scene data extracted from SoftRef bundle `10ba9da1` = `Assets/Scenes/start.unity`
with UnityPy; scripts + raw dump in `tmp/preset-extract/`).

## Problems

1. **Normal can't override a world's saved value.** UI Normal = modifier omitted from args; game only ever
   adds/replaces keys on `-modifier`, so a world saved with Combat Hard (in-game) stays Hard. No `-modifier x normal`
   exists (`WorldModifierOption` has no Normal; Normal slider settings have no keys).
2. **Preset dropdown regression vs WinForms 2.4.** Choosing a preset doesn't fill the fields; key edits don't revert
   to Custom. WinForms `SetFormValuesFromPreset` did both.

## Game facts (verified in code + scene data)

- Server-option keys (`GlobalKeys` < `NonServerOption`: all modifiers + world keys + `preset` marker) live ONLY in the
  world metadata (`.fwl`/`.fwl2`, `World.m_startingGlobalKeys`). `ZoneSystem.Save` strips them from the `.db`
  (`ZoneSystem.cs:994`). Boot: `ZNet.WorldSetup` → `SetStartingGlobalKeys` clears live server-option keys and
  re-adds the starting list. Progression keys (`defeated_*` etc., > `NonServerOption`) are never in that list.
- The `.fwl2` is written only by a world save (`SaveWorldFWLData`); `m_startingKeysChanged` is set but never read.
- CLI args are processed in order (`FejdStartup.cs:780-830`):
  - `-resetmodifiers`: `m_startingGlobalKeys.Clear()`, nothing else. Officially announced in patch 0.217.22
    (2023-10-05, one line; no docs). Not in the bundled server manual. Community guides only since 2026-09.
  - `-preset X`: same Clear(), then applies the preset button's keys + writes `preset x` marker. Buttons exist for
    default/casual/easy/hard/hardcore/hammer/immersive. **`normal` has no button** → keys cleared, then "Missing
    settings for preset: Normal" logged.
  - `-modifier k v`: reads sliders from world, sets one slider, writes its keys (replace same-name key), plus a
    `preset combat_x:deathpenalty_y:...` marker when set manually (`ServerOptionsGUI.cs:81-110`). The marker is
    re-applied at boot (`SetStartingGlobalKeys` → `SetPreset`), a second reason an omitted Normal never wins.
  - `-setkey k`: adds to the same list.
- So `-resetmodifiers` clears modifiers AND world keys together (one list). Our args re-add every checked key, so the
  result equals the UI. Progression untouched.
- Live UI = 5 sliders (Combat, Death, Resources, Raids, Portals) + 5 toggles (playerevents, nobuildcost,
  passivemobs, nomap, fire): exactly ours. Inactive leftovers ignored by `GetComponentsInChildren`: `OLD/Difficulty`
  slider, toggles `Nomap (1)`, `Noportals`, `Portal items`.

### In-game preset label (`ServerOptionsGUI.GetWorldModifierSummary`, `alwaysShort: true`)

Over the world's key list (case-insensitive):
1. Candidates = presets except Default whose every key is in the list (`KeyButton.TryMatch`).
2. Pick the candidate with the MOST keys; ties → first in hierarchy order:
   **Easy, Hard, Hardcore, Casual, Hammer, Immersive** (strict `>`, so the earlier one wins).
3. Remove the chosen preset's keys; if any slider setting/toggle still matches the remainder → "extras".
4. Label: preset + extras → `"<Preset>+"`; preset only → `"<Preset>"`; no preset + extras → Custom; nothing → empty.
Ranking is by KEY count, not field count (Hard 5 keys beats Immersive 3 keys though Immersive sets more fields),
so the port must work on key sets.

### Preset table in field terms (game data; every preset is exactly expressible by our fields)

| Preset (game button) | Combat | Death | Resources | Raids | Portals | Keys |
| --- | --- | --- | --- | --- | --- | --- |
| Default (our "Normal") | – | – | – | – | – | – |
| Easy | Easy | – | – | Less | – | – |
| Hard | Hard | – | – | More | – | – |
| Hardcore | Very Hard | Hardcore | – | More | Hard | nomap |
| Casual | Very Easy | Casual | More | None | Casual | passivemobs, playerevents |
| Hammer | – | – | – | None | – | nobuildcost, passivemobs |
| Immersive | – | – | – | – | Very Hard | nomap, fire |

Checked key-for-key: each preset's `m_keys` == union of the listed slider settings' keys + toggles. WinForms 2.4
table matches all 7 exactly.

### Slider keys (game data)

- Combat: VeryEasy `playerdamage 125, enemydamage 50, enemyspeedsize 90`; Easy `110, 75, 95`; Hard
  `85, 150, 110, enemyleveluprate 120`; VeryHard `70, 200, 120, enemyleveluprate 140`.
- Death: Casual `deathkeepequip, SkillReductionRate 15`; VeryEasy `SkillReductionRate 15`; Easy `50`;
  Hard `DeathDeleteUnequipped, SkillReductionRate 150`; Hardcore `deathdeleteitems, deathskillsreset`.
- Resources: `resourcerate` 50/75/150/200/300. Raids: `eventrate` 0/200/150/60/30. Portals: Casual `teleportall`,
  Hard `nobossportals`, VeryHard `noportals`. Toggles: key == token.

### Wiki discrepancies (https://valheim.weirdgloop.org/w/World_Modifiers) — noted, wiki left as is

1. Combat Easy enemy speed & size: wiki 90%, game 95.
2. Death Casual / Very Easy skill loss: wiki 1%; game `SkillReductionRate 15` → 0.75% if Normal is 5%.
3. No preset→modifier table on the wiki. 4. `-resetmodifiers` unmentioned.

## Decisions (Nuffle, 2026-10-23)

- **D1 Always send `-resetmodifiers`.** Before starting, read the world's latest `.fwl2`; if our args would change or
  remove any non-default value the world currently has, show an OK/Cancel confirmation (Cancel = don't start).
  No stored "acknowledged" flag: after one start + save the file matches, so the check passes on its own.
- **D2 Dropdown items = real presets only.** "Custom (No Preset)" is shown only as the empty-state text when the
  fields match no preset; never selectable.
- **D3 Emulate the game's label** with one Core function + table-driven unit tests ("Hard+", tie order, Custom).
- **D4 Remove** the "Modifiers not working like you expected?…" notice + its FAQ link (and the FAQ URL constant).
- **D5 Leave the wiki.**

## Design

- **Fields are the truth.** Persist modifiers + keys only. Preset is never persisted.
- **Core world-modifier model** (`ValheimServerGUI.Core/Game`, data-driven, one table each, all from game data):
  - per slider: setting token → game keys (Normal = none); toggles: token → key;
  - presets in field terms (the table above), in the game's hierarchy order; a preset's keys are DERIVED by expanding
    its fields (unit test asserts each equals the game's `m_keys` snapshot from this plan);
  - `Expand(fields) → keys`;
  - `Label(fields) → (preset token?, extras bool)` = the port of `GetWorldModifierSummary` over `Expand(fields)`;
    all-default → Normal;
  - `Decode(worldKeys) → fields + unrepresentable keys` (like `KeySlider.TryMatch(World)`: per slider the setting
    whose keys are all present, most-specific first; unknown/console-set keys reported raw; `preset …` marker
    ignored).
- **Launch args:** `-resetmodifiers` first, then `-modifier` for every non-Normal field, then `-setkey` per key, then
  Additional Args (unchanged). Never `-preset`.
- **Start-time check (D1):** compare FIELDS, not keys: decode the world's keys (`FromGameKeys`), then a change is a
  world modifier that is non-Normal and differs from the UI, a world key set and unchecked in the UI, or an
  unrecognized stored key. (A key diff misses Death Very Easy → Casual: Casual's keys contain Very Easy's.)
  Additions-only (world default → UI value) never prompt. Auto-start applies without asking (same rule as the list
  files: non-interactive = the profile wins); each change is logged. Dialog lists each changed setting
  "<Modifier>: <world value> → <UI value>" via `Decode`, raw key for unrepresentable ones. No `.fwl2`, unreadable,
  or pre-modifier world version → no prompt (log the read failure).
- **Dialog (D2/D3):** `DropdownFormField` with `ItemsSource` = real presets (Normal, Easy, Hard, …),
  `Value` = exact-match preset or null; `PlaceholderText` bound to the derived label when not an exact match:
  `"<Preset>+"` or "Custom (No Preset)". Selecting a preset applies its fields. Verify on a real desktop that the
  placeholder renders when `Value` is null after a programmatic change.

## Steps

1. **Core model.** Tables + `Expand`/`Label`/`Decode` as above. Unit tests (table-driven):
   preset expansion == game `m_keys` (all 7); Label cases: all Normal → Normal; each preset exact → itself;
   Hard + Death Easy → Hard+; Hard + Immersive fields → Hard+ (key-count rank); Hammer + Immersive → Hammer+ (tie
   order); Casual + nomap → Casual+; Combat Hard only → Custom; Hardcore minus nomap → Custom (no subset preset);
   Decode round-trips every single-field state; Decode reports a console value (`playerdamage 137`) and an unknown
   key (`nocraftcost`) as unrepresentable; marker ignored.
2. **Legacy prefs expansion.** `WorldPreferences.FromFile`: `Preset` set → expand to fields (∪ saved keys, since v3
   allowed keys with a preset), drop Preset; `ToFile` stops writing it. Unknown token → log + ignore. Tests:
   v2.4 file (preset only), v3 file (preset + keys), unknown token, `normal`.
3. **Args.** `GenerateArgs`: always `-resetmodifiers` first; remove `ValheimServerOptions.WorldPreset` + validation
  + `Validation_WorldPresetInvalid` + `WorldGenPresets.All` if unused; `MainWindowViewModel` passes fields only.
   Tests: order reset < modifiers < keys < additional; never `-preset`; reset present with no prefs file too.
4. **`.fwl2` reader.** Generalize the reader in `WorldPlayerHistory` into one metadata read returning starting keys +
   history (it already walks the keys); history callers unchanged. Unit test on the save fixtures + a fixture with
   modifiers.
5. **Start-time confirmation.** Core: `WorldModifierCheck(saveDir, worldName, fields) → changes[]` (thin: latest
   committed `.fwl2` via the existing finder). App start flow: non-empty → `MessageBox` OK/Cancel listing changes;
   Cancel aborts the start. New strings (`Prompt_WorldModifiersChange_Title/_Message`, line format). Tests in
   `StartServerFlowTests`: prompt on Hard world + Normal UI; no prompt when world matches; no prompt for additions
   only; Cancel doesn't start; no world file → no prompt.
6. **Dialog VM + view.** Derived preset (Value + placeholder), apply on select, delete `_suppressRevert`,
   `OnModifierChanged` revert, `IsPresetSelected`, `CustomPreset`, token state. Remove the notice + link
   (`WorldPreferences_NotWorking*`, `OpenWorldModifiersHelp`, `UrlHelpWorldModifiers`). Replace the 4 World* VM
   tests: select fills fields; edit modifier or key → placeholder Custom/"Hard+"; edit back → preset reappears; load
   shows derived preset; save persists fields only; Restore Defaults → Normal. `./scripts/gen-test-strings.sh`.
   Real-desktop check (Xvfb screenshot) of the placeholder state.
7. **Integration (live; Linux configs + Windows VMs).**
   - *Bug repro + fix:* boot fresh world with Combat Hard → graceful stop → boot with UI all-Normal → stop → `.fwl2`
     starting keys contain no `enemydamage`/`preset` key; log has "Resetting world modifiers". Also assert the
     start-time check reported the change before the second boot and reports none before a third. A/B: revert only
     the `-resetmodifiers` line (worktree) → test FAILS. Record the A/B in CLAUDE.local.md.
   - *Preset guard:* per preset, `.fwl2` keys after `-preset X` (via AdditionalArgs) == `Expand(fields)` (ignoring
     marker). Catches game updates to presets. Measure cost first (2 boots × 7); parallel savedirs or Linux-only if
     slow.
8. **Docs/release.** Sibling docs-site FAQ entry ("isn't correctly displaying my world modifiers") rewritten or
   removed; release note: VSG now makes the world match World Preferences, with a confirmation when that changes
   existing settings.
9. `./scripts/validate.sh`, `./scripts/integration.sh`, `./scripts/integration-windows.sh`; update CLAUDE.local.md +
   project memory.

## Resolved questions

- Q6 (agreed): unrecognized stored keys are listed raw in the prompt.
- Q7 (agreed): a server killed before its first save prompts again next start (the file still differs).
- Real in-game worlds store `preset combat_default:deathpenalty_default:...` even when all Normal (fixture
  `TWReleaseWorld.fwl2`); the marker is ignored everywhere.
