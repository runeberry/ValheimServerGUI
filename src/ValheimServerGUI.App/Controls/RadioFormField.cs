using System;
using Avalonia;
using Avalonia.Data;

namespace ValheimServerGUI.App.Controls;

/// <summary>Functional code for <see cref="RadioFormField"/> (properties + value change), split from the
/// XAML-paired code-behind in <c>RadioFormField.axaml.cs</c>.</summary>
public partial class RadioFormField : IFormField<bool>
{
    public static readonly StyledProperty<bool> ValueProperty =
        AvaloniaProperty.Register<RadioFormField, bool>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> GroupNameProperty =
        AvaloniaProperty.Register<RadioFormField, string?>(nameof(GroupName));

    /// <summary>True when this radio is selected (two-way ↔ the inner <c>RadioButton.IsChecked</c>).</summary>
    public bool Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>The radio group these fields coordinate within.</summary>
    public string? GroupName
    {
        get => GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    /// <inheritdoc />
    public event EventHandler<bool>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) ValueChanged?.Invoke(this, Value);
    }
}
