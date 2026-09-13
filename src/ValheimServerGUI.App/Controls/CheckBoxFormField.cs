using System;
using Avalonia;
using Avalonia.Data;

namespace ValheimServerGUI.App.Controls;

/// <summary>Functional code for <see cref="CheckBoxFormField"/> (properties + value change), split from the
/// XAML-paired code-behind in <c>CheckBoxFormField.axaml.cs</c>.</summary>
public partial class CheckBoxFormField : IFormField<bool>
{
    public static readonly StyledProperty<bool> ValueProperty =
        AvaloniaProperty.Register<CheckBoxFormField, bool>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public bool Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <inheritdoc />
    public event EventHandler<bool>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) ValueChanged?.Invoke(this, Value);
    }
}
