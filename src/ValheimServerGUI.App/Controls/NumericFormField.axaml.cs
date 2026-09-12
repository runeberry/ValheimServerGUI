using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's integer field: a caption + help glyph over the compact vertically-stacked spinner. Replaces
/// raw <see cref="Avalonia.Controls.NumericUpDown"/> so numeric inputs are consistent everywhere.
/// </summary>
public partial class NumericFormField : FormFieldBase
{
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<NumericFormField, int>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MinimumProperty =
        AvaloniaProperty.Register<NumericFormField, int>(nameof(Minimum));

    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<NumericFormField, int>(nameof(Maximum), defaultValue: int.MaxValue);

    public NumericFormField() => AvaloniaXamlLoader.Load(this);

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
}
