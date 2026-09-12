using System.Collections;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's dropdown field: a caption + help glyph over a combo box. Replaces raw
/// <see cref="Avalonia.Controls.ComboBox"/> in the data-bound views.
/// </summary>
public partial class DropdownFormField : FormFieldBase
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<DropdownFormField, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DropdownFormField, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Text shown when nothing is selected (e.g. a disabled "-- No worlds --" empty state).</summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<DropdownFormField, string?>(nameof(PlaceholderText));

    public DropdownFormField() => AvaloniaXamlLoader.Load(this);

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>The selected item (two-way).</summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}
