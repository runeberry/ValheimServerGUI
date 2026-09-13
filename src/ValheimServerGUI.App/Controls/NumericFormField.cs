using System;
using Avalonia;
using Avalonia.Data;

namespace ValheimServerGUI.App.Controls;

/// <summary>Functional code for <see cref="NumericFormField"/> (properties + value change), split from the
/// XAML-paired code-behind in <c>NumericFormField.axaml.cs</c>.</summary>
public partial class NumericFormField : IFormField<int>
{
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<NumericFormField, int>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MinimumProperty =
        AvaloniaProperty.Register<NumericFormField, int>(nameof(Minimum));

    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<NumericFormField, int>(nameof(Maximum), defaultValue: int.MaxValue);

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <inheritdoc />
    public event EventHandler<int>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) ValueChanged?.Invoke(this, Value);
    }
}
