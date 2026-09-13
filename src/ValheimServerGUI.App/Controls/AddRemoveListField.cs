using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Data;

namespace ValheimServerGUI.App.Controls;

/// <summary>Functional code for <see cref="AddRemoveListField"/> (properties + commands), split from the
/// XAML-paired code-behind in <c>AddRemoveListField.axaml.cs</c>.</summary>
public partial class AddRemoveListField
{
    public static readonly StyledProperty<string?> LabelTextProperty =
        AvaloniaProperty.Register<AddRemoveListField, string?>(nameof(LabelText));

    public static readonly StyledProperty<string?> HelpTextProperty =
        AvaloniaProperty.Register<AddRemoveListField, string?>(nameof(HelpText));

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AddRemoveListField, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<AddRemoveListField, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<double> ListHeightProperty =
        AvaloniaProperty.Register<AddRemoveListField, double>(nameof(ListHeight), defaultValue: double.NaN);

    public static readonly StyledProperty<ICommand?> AddCommandProperty =
        AvaloniaProperty.Register<AddRemoveListField, ICommand?>(nameof(AddCommand));

    public static readonly StyledProperty<ICommand?> EditCommandProperty =
        AvaloniaProperty.Register<AddRemoveListField, ICommand?>(nameof(EditCommand));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
        AvaloniaProperty.Register<AddRemoveListField, ICommand?>(nameof(RemoveCommand));

    public static readonly StyledProperty<bool> AddEnabledProperty =
        AvaloniaProperty.Register<AddRemoveListField, bool>(nameof(AddEnabled), defaultValue: true);

    public static readonly StyledProperty<bool> EditEnabledProperty =
        AvaloniaProperty.Register<AddRemoveListField, bool>(nameof(EditEnabled), defaultValue: true);

    public static readonly StyledProperty<bool> RemoveEnabledProperty =
        AvaloniaProperty.Register<AddRemoveListField, bool>(nameof(RemoveEnabled), defaultValue: true);

    public string? LabelText { get => GetValue(LabelTextProperty); set => SetValue(LabelTextProperty, value); }
    public string? HelpText { get => GetValue(HelpTextProperty); set => SetValue(HelpTextProperty, value); }
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public double ListHeight { get => GetValue(ListHeightProperty); set => SetValue(ListHeightProperty, value); }

    public ICommand? AddCommand { get => GetValue(AddCommandProperty); set => SetValue(AddCommandProperty, value); }
    public ICommand? EditCommand { get => GetValue(EditCommandProperty); set => SetValue(EditCommandProperty, value); }
    public ICommand? RemoveCommand { get => GetValue(RemoveCommandProperty); set => SetValue(RemoveCommandProperty, value); }

    public bool AddEnabled { get => GetValue(AddEnabledProperty); set => SetValue(AddEnabledProperty, value); }
    public bool EditEnabled { get => GetValue(EditEnabledProperty); set => SetValue(EditEnabledProperty, value); }
    public bool RemoveEnabled { get => GetValue(RemoveEnabledProperty); set => SetValue(RemoveEnabledProperty, value); }
}
