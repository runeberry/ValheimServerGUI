using System.Collections.Generic;
using Avalonia.Controls;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// One button on a <see cref="MessageBoxWindow"/>: a caption, the value the window closes with when it is
/// clicked, and the default (Enter) / cancel (Esc) roles.
/// </summary>
public sealed class MessageBoxButton
{
    public MessageBoxButton(string caption, object? result = null, bool isDefault = false, bool isCancel = false)
    {
        Caption = caption;
        Result = result;
        IsDefault = isDefault;
        IsCancel = isCancel;
    }

    public string Caption { get; }
    public object? Result { get; }
    public bool IsDefault { get; }
    public bool IsCancel { get; }
}

/// <summary>
/// The single reusable modal for title + message + a row of buttons — the Avalonia replacement for WinForms
/// <c>MessageBox.Show</c>. It carries no per-dialog styling: callers describe the buttons (caption + result)
/// and the window closes with the chosen result. Prefer the <see cref="MessageBox"/> facade over constructing
/// this directly.
/// </summary>
public partial class MessageBoxWindow : DialogWindow
{
    public MessageBoxWindow()
    {
        InitializeComponent();
    }

    public MessageBoxWindow(string title, string message, IReadOnlyList<MessageBoxButton> buttons) : this()
    {
        Title = title;
        MessageText.Text = message;

        foreach (var spec in buttons)
        {
            var button = new Button
            {
                Content = spec.Caption,
                MinWidth = 80,
                IsDefault = spec.IsDefault,
                IsCancel = spec.IsCancel,
            };
            var result = spec.Result;
            button.Click += (_, _) =>
            {
                Result = result;
                Close(result);
            };
            ButtonBar.Children.Add(button);
        }
    }

    /// <summary>The chosen button's result, mirrored here so an ownerless (non-modal) show can read it on close.</summary>
    public object? Result { get; private set; }
}
