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

    public DropdownFormField() => AvaloniaXamlLoader.Load(this);

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
