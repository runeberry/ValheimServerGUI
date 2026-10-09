# ValheimServerGUI — project notes

## UI: prefer our specialized controls over stock Fluent controls

The app ships a family of custom, data-bound controls in `./src/ValheimServerGUI.App/Controls`
(namespace `ValheimServerGUI.App.Controls`, conventionally aliased `c:` in XAML). They carry the app's
styling, the consistent label + help-glyph layout, and the `IFormField<T>` binding contract. **Always
reach for the specialized control instead of the stock Avalonia/Fluent one** when adding or editing views;
a raw Fluent control in a data-bound view is a smell and breaks visual consistency.

Common replacements (stock → ours):

| Stock control | Use instead | Notes |
| --- | --- | --- |
| `CheckBox` | `c:CheckBoxFormField` | `LabelText` caption + `HelpText` glyph; `Value` (two-way) |
| `TextBox` | `c:TextFormField` | `LabelText`/`Watermark`/`MaxLength`; `Value` |
| `TextBox` (file/folder path) | `c:FilenameFormField` | `TextFormField` with a built-in browse end-cap |
| `ComboBox` | `c:DropdownFormField` | `ItemsSource` + `Value`; `PlaceholderText` empty state |
| `NumericUpDown` | `c:NumericFormField` | `Minimum`/`Maximum`; compact spinner skin |
| `NumericUpDown` (seconds) | `c:DurationFormField` | shows amount + unit, binds seconds |
| `RadioButton` | `c:RadioFormField` | `GroupName` + `Value` |
| `DataGrid` / `ListBox` | `c:DataListView` | header/footer-capable data list |
| `Label` / caption `TextBlock` | `c:LabelField` | field-style label |
| bordered/`HeaderedContentControl` section | `c:GroupBox` | etched WinForms-style group |
| link `Button`/`HyperlinkButton` | `c:HyperlinkLabel` | clickable link text |
| icon `Image` | `c:IconImage` | app icon lookup |
| icon `Button` | `c:IconButton` | generic icon button |
| icon `Button` (specific verbs) | `c:OpenButton`, `c:RefreshButton`, `c:SettingsButton`, `c:EditButton`, `c:CopyButton` | purpose-built |
| help `?` glyph / tooltip | `c:HelpLabel` (usually a field's `HelpText`) | consistent help affordance |

If none fits a genuinely new need, add a new control to `Controls/` following the existing pattern
(`FormFieldBase` + `IFormField<T>` for inputs) rather than dropping a stock control into a view.

## Localization: all user-facing text lives in `Strings.resx`

Every string a user reads comes from `src/ValheimServerGUI.Localization/Strings.resx` (English, the neutral
culture) through the generated public `Strings` class. Never add literal copy to a view, view model or Core
message.

- **XAML:** `{x:Static loc:Strings.Key}` with `xmlns:loc="using:ValheimServerGUI.Localization"`. Formatted
  bindings use `StringFormat={x:Static loc:Strings.Key}`. `LocalizationTests.Xaml_has_no_literal_copy` fails
  on any literal text attribute (only the `?` help glyph and brand names are allowed).
- **C#:** `Strings.Key`, or `string.Format(Strings.Key, args)` with `{0}`-style placeholders (keep any number
  or date format inside the placeholder, e.g. `{0:F0}ms`). A resource is not a compile-time constant, so
  optional parameters default to `null` and fall back to `Strings.*` (see `MessageBox`).
- **Enums shown to users** go through `EnumDisplayConverter.ToText` (status/role columns) or the dropdowns'
  `DisplayMemberBinding`; the enum value stays the stored token. Add new displayed enums there; a test fails
  if any value lacks a mapping.
- **Display text that maps back to a token** (`WorldGenDisplay`) builds its token↔display table from
  `Strings` on each call and uses the same table in both directions. Never compare against display text.
- **Plurals:** separate `_One` / `_Other` keys (see `RelativeTime_*`).
- **Key naming:** flat, area-prefixed, underscores, grouped under `<!-- Area -->` comments in the resx:
  `MainWindow_Menu_File`, `ServerControls_Port_Label` / `_Help`, `Prompt_RemoveProfile_Title` / `_Message`,
  `Validation_PortInUse`, `Role_Admin`, `Common_OK`.
- **Copy moves verbatim** (see the verbatim-UI-copy rule): extraction never rewords English.

**Never localize:** log messages (logger calls, `PlayerListImportService.LogLines`, level prefixes; they go
into bug/crash reports), crash-report context labels and bug-report field keys, server log-parse patterns,
CLI args and env vars, world-gen/role/platform tokens, list file names and header comments, paths, URLs,
registry keys, the Steam App ID, `.desktop` content, `userprefs.txt` keys, developer-facing argument guards,
and brand names (`AppConstants.ProductName`, the About product name and copyright, GitHub, Discord, and the
platform names Steam/Xbox/PlayStation/Nintendo).

**Test culture.** `Strings.qps-ploc.resx` is a generated key-echo culture: every value is `⟦Key⟧`, keeping the
English placeholders (`⟦Import_Confirm {0}⟧`). Every test project pins it as the UI culture (`CurrentCulture`
stays en-US), so tests assert against the accessor (`Assert.Equal(Strings.Players_EmptyText, vm.EmptyText)`),
never English. After adding, renaming or removing a key, or changing an English value's placeholders, run:

```bash
./scripts/gen-test-strings.sh
```

`LocalizationTests` fails if the two files drift. The test culture never ships: App's
`ExcludeTestCultureFromPublish` target drops the `qps-ploc/` satellite from publish output (and so from the
single-file bundle).

**QA:** `VSG_LANG=<culture>` overrides the UI culture at startup. `VSG_LANG=qps-ploc` shows keys in place of
text, so any plain English on screen (other than user data, logs and brand names) is a missed string. In that
mode Avalonia treats a key's first `_` as an access-key mnemonic; that is cosmetic and only affects QA. The
UI culture is restart-to-switch.
