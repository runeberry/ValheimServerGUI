using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
/// The app's path field: a caption + help glyph over a text box whose action buttons live <em>inside</em> the
/// box border as end-caps — a "Browse…" button (a real file/folder picker via the window's storage provider)
/// and, when <see cref="ShowOpenFolder"/> is set, an open-folder button bound to <see cref="OpenFolderCommand"/>.
/// Each end-cap is optional (<see cref="ShowBrowse"/>/<see cref="ShowOpenFolder"/>) so the whole field reads as
/// a single outlined control. Mirrors the WinForms <c>FilenameFormField</c>, which likewise owned its buttons.
/// </summary>
public partial class FilenameFormField : FormFieldBase, IFormField<string?>
{
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<FilenameFormField, string?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<FileSelectMode> SelectModeProperty =
        AvaloniaProperty.Register<FilenameFormField, FileSelectMode>(nameof(SelectMode));

    public static readonly StyledProperty<bool> ShowBrowseProperty =
        AvaloniaProperty.Register<FilenameFormField, bool>(nameof(ShowBrowse), defaultValue: true);

    public static readonly StyledProperty<bool> ShowOpenFolderProperty =
        AvaloniaProperty.Register<FilenameFormField, bool>(nameof(ShowOpenFolder));

    public static readonly StyledProperty<ICommand?> OpenFolderCommandProperty =
        AvaloniaProperty.Register<FilenameFormField, ICommand?>(nameof(OpenFolderCommand));

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

    /// <summary>Whether the built-in "Browse…" end-cap is shown (default true).</summary>
    public bool ShowBrowse
    {
        get => GetValue(ShowBrowseProperty);
        set => SetValue(ShowBrowseProperty, value);
    }

    /// <summary>Whether an open-folder end-cap is shown after Browse (default false). Wire
    /// <see cref="OpenFolderCommand"/> to give it an action.</summary>
    public bool ShowOpenFolder
    {
        get => GetValue(ShowOpenFolderProperty);
        set => SetValue(ShowOpenFolderProperty, value);
    }

    /// <summary>Command invoked by the open-folder end-cap (the owner keeps the shell dependency).</summary>
    public ICommand? OpenFolderCommand
    {
        get => GetValue(OpenFolderCommandProperty);
        set => SetValue(OpenFolderCommandProperty, value);
    }

    /// <inheritdoc />
    public event EventHandler<string?>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) ValueChanged?.Invoke(this, Value);
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
