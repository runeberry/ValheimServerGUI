using System;
using System.Collections;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's labelled single-select list (the WinForms <c>SelectListField</c> equivalent): a
/// <see cref="FormFieldBase.LabelText"/> caption + help glyph over a list box. Thin and MVVM-idiomatic — the
/// ViewModel owns the items via <see cref="ItemsSource"/> and the selection via <see cref="Value"/>; the
/// control does not replicate the WinForms imperative add/remove API.
/// </summary>
public partial class SelectListField : FormFieldBase, IFormField<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<SelectListField, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<SelectListField, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Explicit height for the list box. NaN = auto.</summary>
    public static readonly StyledProperty<double> ListHeightProperty =
        AvaloniaProperty.Register<SelectListField, double>(nameof(ListHeight), defaultValue: double.NaN);

    public SelectListField() => AvaloniaXamlLoader.Load(this);

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
