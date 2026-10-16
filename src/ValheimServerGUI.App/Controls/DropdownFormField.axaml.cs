using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// An item that draws a divider line in a <see cref="DropdownFormField"/> list. It cannot be selected.
/// </summary>
public sealed class DropdownSeparator
{
    public static readonly DropdownSeparator Instance = new();

    private DropdownSeparator()
    {
    }
}

/// <summary>
/// The app's dropdown field: a caption + help glyph over a combo box. Replaces raw
/// <see cref="Avalonia.Controls.ComboBox"/> in the data-bound views. Items may include
/// <see cref="DropdownSeparator.Instance"/> to group them.
/// </summary>
public partial class DropdownFormField : FormFieldBase, IFormField<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<DropdownFormField, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DropdownFormField, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Text shown when nothing is selected (e.g. a disabled "-- No worlds --" empty state).</summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<DropdownFormField, string?>(nameof(PlaceholderText));

    public DropdownFormField()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<ComboBox>("Combo")!.ContainerPrepared += OnContainerPrepared;
    }

    // Containers can be recycled between items, so every prepared container is set for the item it now holds.
    private static void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is not ComboBoxItem item) return;

        var isSeparator = item.DataContext is DropdownSeparator;
        item.IsEnabled = !isSeparator;
        item.Focusable = !isSeparator;
        if (isSeparator)
        {
            item.ContentTemplate = null;
            item.Content = new Separator();
        }
    }

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
        if (change.Property != ValueProperty) return;

        if (Value is DropdownSeparator)
        {
            SetCurrentValue(ValueProperty, change.OldValue);
            return;
        }
        ValueChanged?.Invoke(this, Value);
    }
}
