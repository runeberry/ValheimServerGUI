using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's boolean radio field: a radio button whose caption is <see cref="FormFieldBase.LabelText"/>, with
/// the help glyph beside it. Grouping is by <see cref="GroupName"/> — Avalonia's <c>RadioButton.GroupName</c>
/// already coordinates the selection across containers, so no cross-form discovery is needed (unlike the
/// WinForms version). Replaces raw <see cref="Avalonia.Controls.RadioButton"/> in data-bound views.
/// </summary>
public partial class RadioFormField : FormFieldBase, IFormField<bool>
{
    public static readonly StyledProperty<bool> ValueProperty =
        AvaloniaProperty.Register<RadioFormField, bool>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> GroupNameProperty =
        AvaloniaProperty.Register<RadioFormField, string?>(nameof(GroupName));

    public RadioFormField() => AvaloniaXamlLoader.Load(this);

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
