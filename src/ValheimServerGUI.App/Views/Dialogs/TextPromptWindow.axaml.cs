using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>Generic single-line text prompt (profile names, character names). Closes with the text, or null on cancel.</summary>
public partial class TextPromptWindow : Window
{
    private Func<string, string?>? _validator;

    public TextPromptWindow()
    {
        InitializeComponent();
    }

    public TextPromptWindow(string title, string prompt, string? initial = null,
        int maxLength = 0, Func<string, string?>? validator = null) : this()
    {
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = initial ?? string.Empty;
        if (maxLength > 0) InputBox.MaxLength = maxLength;
        _validator = validator;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Accept();
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Accept();

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private void Accept()
    {
        var value = InputBox.Text ?? string.Empty;
        var error = _validator?.Invoke(value);
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.IsVisible = true;
            return;
        }

        Close(value);
    }
}
