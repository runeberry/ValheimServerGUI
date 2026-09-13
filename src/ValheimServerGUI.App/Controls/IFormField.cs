using System;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The Avalonia port of the WinForms <c>IFormField</c> contract: the label + help caption every data-bound
/// field carries. <see cref="FormFieldBase"/> implements this; concrete fields additionally implement
/// <see cref="IFormField{T}"/> for their typed value. Keeping the contract lets a consumer treat any field
/// uniformly (read/set its caption + help text) regardless of the underlying input.
/// </summary>
public interface IFormField
{
    /// <summary>The field caption.</summary>
    string? LabelText { get; set; }

    /// <summary>Tooltip text for the "?" help glyph. When null/empty the glyph is hidden.</summary>
    string? HelpText { get; set; }
}

/// <summary>
/// A data-bound field with a typed, two-way <see cref="Value"/>. Concrete fields raise
/// <see cref="ValueChanged"/> whenever the value changes (driven off the <c>ValueProperty</c> change), the
/// Avalonia equivalent of the WinForms <c>ValueChanged</c> event.
/// </summary>
public interface IFormField<T> : IFormField
{
    /// <summary>The field's current value (two-way bindable).</summary>
    T Value { get; set; }

    /// <summary>Raised after <see cref="Value"/> changes.</summary>
    event EventHandler<T> ValueChanged;
}
