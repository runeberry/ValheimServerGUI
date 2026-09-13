using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's read-only caption + value row (the WinForms <c>LabelField</c> equivalent): a right-aligned
/// <see cref="FormFieldBase.LabelText"/> caption, a selectable read-only <see cref="Value"/>, the help glyph,
/// and an optional <see cref="TrailingContent"/> slot (e.g. a <see cref="CopyButton"/>). Replaces the raw
/// caption/value grids in the Server Details and Player Details surfaces.
/// </summary>
public partial class LabelField : FormFieldBase, IFormField<string>
{
    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<LabelField, string>(nameof(Value), defaultValue: string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Width of the caption column (default 130, matching the WinForms detail rows).</summary>
    public static readonly StyledProperty<GridLength> LabelWidthProperty =
        AvaloniaProperty.Register<LabelField, GridLength>(nameof(LabelWidth), defaultValue: new GridLength(130));

    /// <summary>Optional control rendered after the value (e.g. a copy button).</summary>
    public static readonly StyledProperty<object?> TrailingContentProperty =
        AvaloniaProperty.Register<LabelField, object?>(nameof(TrailingContent));

    public LabelField() => AvaloniaXamlLoader.Load(this);

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public GridLength LabelWidth
    {
        get => GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    /// <inheritdoc />
    public event EventHandler<string>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) ValueChanged?.Invoke(this, Value);
    }
}
