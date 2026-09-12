using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace ValheimServerGUI.App.Controls;

/// <summary>How a <see cref="FilenameFormField"/>'s browse button picks a path.</summary>
public enum FileSelectMode
{
    File,
    Directory,
}

/// <summary>
/// The app's path field: a caption + help glyph over a text box with a built-in "…" browse button (a real
/// file/folder picker via the window's storage provider) plus an optional trailing slot for an extra action
/// such as an open-folder button. Mirrors the WinForms <c>FilenameFormField</c>, which likewise owned its
/// browse button.
/// </summary>
public partial class FilenameFormField : FormFieldBase
{
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<FilenameFormField, string?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<FileSelectMode> SelectModeProperty =
        AvaloniaProperty.Register<FilenameFormField, FileSelectMode>(nameof(SelectMode));

    public static readonly StyledProperty<object?> TrailingContentProperty =
        AvaloniaProperty.Register<FilenameFormField, object?>(nameof(TrailingContent));

    public FilenameFormField() => AvaloniaXamlLoader.Load(this);

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public FileSelectMode SelectMode
    {
        get => GetValue(SelectModeProperty);
        set => SetValue(SelectModeProperty, value);
    }

    /// <summary>Optional control rendered after the browse button (e.g. an open-folder button).</summary>
    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    private async void Browse(object? sender, RoutedEventArgs e) => await BrowseAsync();

    private async Task BrowseAsync()
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null) return;

        var start = await SuggestedStartLocation(storage);
        string? picked;

        if (SelectMode == FileSelectMode.Directory)
        {
            var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LabelText,
                AllowMultiple = false,
                SuggestedStartLocation = start,
            });
            picked = folders.FirstOrDefault()?.TryGetLocalPath();
        }
        else
        {
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = LabelText,
                AllowMultiple = false,
                SuggestedStartLocation = start,
            });
            picked = files.FirstOrDefault()?.TryGetLocalPath();
        }

        if (!string.IsNullOrEmpty(picked)) Value = picked;
    }

    // Open the picker at the current value's folder when it points at a real location.
    private async Task<IStorageFolder?> SuggestedStartLocation(IStorageProvider storage)
    {
        if (string.IsNullOrWhiteSpace(Value)) return null;

        var dir = SelectMode == FileSelectMode.Directory
            ? Value
            : System.IO.Path.GetDirectoryName(Value);

        if (string.IsNullOrWhiteSpace(dir)) return null;

        try
        {
            return await storage.TryGetFolderFromPathAsync(dir);
        }
        catch
        {
            return null;
        }
    }
}
