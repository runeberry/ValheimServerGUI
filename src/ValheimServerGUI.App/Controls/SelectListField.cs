using System;
using System.Collections;
using Avalonia;
using Avalonia.Data;

namespace ValheimServerGUI.App.Controls;

/// <summary>Functional code for <see cref="SelectListField"/> (properties + value change), split from the
/// XAML-paired code-behind in <c>SelectListField.axaml.cs</c>.</summary>
public partial class SelectListField : IFormField<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<SelectListField, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<SelectListField, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Explicit height for the list box. NaN = auto.</summary>
    public static readonly StyledProperty<double> ListHeightProperty =
        AvaloniaProperty.Register<SelectListField, double>(nameof(ListHeight), defaultValue: double.NaN);

    /// <summary>The selected item (two-way ↔ the inner list box's selection).</summary>
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

    public double ListHeight
    {
        get => GetValue(ListHeightProperty);
        set => SetValue(ListHeightProperty, value);
    }

    /// <inheritdoc />
    public event EventHandler<object?>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) ValueChanged?.Invoke(this, Value);
    }
}
