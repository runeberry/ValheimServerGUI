using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>A time unit the <see cref="DurationFormField"/> can express its seconds value in.</summary>
public enum DurationUnit
{
    Seconds,
    Minutes,
    Hours,
    Days,
}

/// <summary>
/// A duration field for settings the server stores as a whole number of seconds (save interval, backup
/// intervals). It presents that value through the compact spinner plus a unit dropdown so a user reads and
/// edits it as "30 Minutes" rather than "1800" — but <see cref="Value"/> stays canonical seconds, so the
/// binding surface is a drop-in replacement for the old <see cref="NumericFormField"/> on these fields.
///
/// <see cref="Minimum"/>/<see cref="Maximum"/> are the seconds bounds; the spinner's own bounds are derived
/// per unit so the seconds constraint is always honoured. On an external <see cref="Value"/> change the field
/// picks the largest unit that divides it evenly (1800 → 30 Minutes, 43200 → 12 Hours); changing the unit
/// keeps the shown number and re-scales the duration (clamped into range), which is the least-surprising
/// behaviour for whole-number inputs.
/// </summary>
public partial class DurationFormField : FormFieldBase, IFormField<int>
{
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<DurationFormField, int>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MinimumProperty =
        AvaloniaProperty.Register<DurationFormField, int>(nameof(Minimum));

    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<DurationFormField, int>(nameof(Maximum), defaultValue: int.MaxValue);

    public static readonly StyledProperty<DurationUnit> SelectedUnitProperty =
        AvaloniaProperty.Register<DurationFormField, DurationUnit>(nameof(SelectedUnit));

    public static readonly StyledProperty<int> DisplayAmountProperty =
        AvaloniaProperty.Register<DurationFormField, int>(nameof(DisplayAmount));

    public static readonly StyledProperty<int> NumericMinimumProperty =
        AvaloniaProperty.Register<DurationFormField, int>(nameof(NumericMinimum));

    public static readonly StyledProperty<int> NumericMaximumProperty =
        AvaloniaProperty.Register<DurationFormField, int>(nameof(NumericMaximum), defaultValue: int.MaxValue);

    // Guards the two-way sync between Value (seconds) and the DisplayAmount/SelectedUnit shown to the user,
    // so a programmatic write on one side does not bounce back and fight the other.
    private bool _syncing;

    public DurationFormField()
    {
        AvaloniaXamlLoader.Load(this);
        SyncFromValue();
    }

    /// <summary>The duration in seconds — the canonical value the server config stores.</summary>
    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Lower bound, in seconds.</summary>
    public int Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Upper bound, in seconds.</summary>
    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>The unit the value is currently displayed in.</summary>
    public DurationUnit SelectedUnit
    {
        get => GetValue(SelectedUnitProperty);
        set => SetValue(SelectedUnitProperty, value);
    }

    /// <summary>The number shown in the spinner (in <see cref="SelectedUnit"/> units).</summary>
    public int DisplayAmount
    {
        get => GetValue(DisplayAmountProperty);
        set => SetValue(DisplayAmountProperty, value);
    }

    /// <summary>Spinner lower bound, derived from <see cref="Minimum"/> for the current unit.</summary>
    public int NumericMinimum
    {
        get => GetValue(NumericMinimumProperty);
        private set => SetValue(NumericMinimumProperty, value);
    }

    /// <summary>Spinner upper bound, derived from <see cref="Maximum"/> for the current unit.</summary>
    public int NumericMaximum
    {
        get => GetValue(NumericMaximumProperty);
        private set => SetValue(NumericMaximumProperty, value);
    }

    /// <summary>The unit choices offered in the dropdown.</summary>
    public IReadOnlyList<DurationUnit> Units { get; } =
        new[] { DurationUnit.Seconds, DurationUnit.Minutes, DurationUnit.Hours, DurationUnit.Days };

    /// <inheritdoc />
    public event EventHandler<int>? ValueChanged;

    private static int SecondsPerUnit(DurationUnit unit) => unit switch
    {
        DurationUnit.Days => 86400,
        DurationUnit.Hours => 3600,
        DurationUnit.Minutes => 60,
        _ => 1,
    };

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            ValueChanged?.Invoke(this, Value);
            if (!_syncing) SyncFromValue();
        }
        else if (change.Property == SelectedUnitProperty || change.Property == DisplayAmountProperty)
        {
            if (!_syncing) SyncToValue();
        }
        else if (change.Property == MinimumProperty || change.Property == MaximumProperty)
        {
            RecalcBounds();
        }
    }

    // Value (seconds) → the unit + amount the user sees. Picks the largest unit that divides the value
    // evenly so it reads as cleanly as possible.
    private void SyncFromValue()
    {
        _syncing = true;
        try
        {
            var unit = CleanestUnit(Value);
            SelectedUnit = unit;
            RecalcBounds();
            DisplayAmount = Value / SecondsPerUnit(unit);
        }
        finally
        {
            _syncing = false;
        }
    }

    // The user's amount + unit → Value (seconds), keeping the shown number within the unit's derived bounds.
    private void SyncToValue()
    {
        _syncing = true;
        try
        {
            RecalcBounds();
            var amount = Math.Clamp(DisplayAmount, NumericMinimum, NumericMaximum);
            if (amount != DisplayAmount) DisplayAmount = amount;
            Value = amount * SecondsPerUnit(SelectedUnit);
        }
        finally
        {
            _syncing = false;
        }
    }

    // Express the seconds bounds in the current unit: round the minimum up and the maximum down so every
    // reachable amount maps back to a value inside [Minimum, Maximum].
    private void RecalcBounds()
    {
        var per = SecondsPerUnit(SelectedUnit);
        var min = Minimum <= 0 ? 0 : (int)Math.Ceiling((double)Minimum / per);
        var max = (int)Math.Min(int.MaxValue, (long)Maximum / per);
        NumericMinimum = min;
        NumericMaximum = Math.Max(min, max);
    }

    private static DurationUnit CleanestUnit(int seconds)
    {
        if (seconds <= 0) return DurationUnit.Seconds;

        foreach (var unit in new[] { DurationUnit.Days, DurationUnit.Hours, DurationUnit.Minutes })
        {
            if (seconds % SecondsPerUnit(unit) == 0) return unit;
        }

        return DurationUnit.Seconds;
    }
}
