using System;
using System.Collections;
using Avalonia;
using Avalonia.Data;

namespace ValheimServerGUI.App.Controls;

/// <summary>Functional code for <see cref="DropdownFormField"/> (properties + value change), split from the
/// XAML-paired code-behind in <c>DropdownFormField.axaml.cs</c>.</summary>
public partial class DropdownFormField : IFormField<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<DropdownFormField, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DropdownFormField, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Text shown when nothing is selected (e.g. a disabled "-- No worlds --" empty state).</summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<DropdownFormField, string?>(nameof(PlaceholderText));

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

    /// <inheritdoc />
    public event EventHandler<object?>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) ValueChanged?.Invoke(this, Value);
    }
}
