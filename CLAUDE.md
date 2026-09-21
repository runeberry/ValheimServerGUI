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
