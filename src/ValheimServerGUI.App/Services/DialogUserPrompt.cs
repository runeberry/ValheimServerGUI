using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// Window-backed <see cref="IUserPrompt"/>. The Core seam is synchronous (it replaces a blocking WinForms
/// <c>MessageBox</c>), while Avalonia dialogs are async, so this bridges the two: the confirmation is shown
/// on the UI thread and the caller is blocked on a nested dispatcher frame until the user answers. Safe to
/// call from any thread — a background caller is marshalled onto the UI thread first (which is where the
/// exception-handler path usually originates).
/// </summary>
internal sealed class DialogUserPrompt : IUserPrompt
{
    public bool Confirm(string message, string title)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return ShowConfirmSync(message, title);

        return Dispatcher.UIThread.Invoke(() => ShowConfirmSync(message, title));
    }

    // Runs on the UI thread. Pumps a nested dispatcher frame so the modal's async result becomes a
    // synchronous return without deadlocking the UI thread on itself.
    private static bool ShowConfirmSync(string message, string title)
    {
        var owner = WindowLocator.ActiveWindow;
        var dialog = new ConfirmWindow(title, message);

        var frame = new DispatcherFrame();
        var result = false;

        Task<bool> resultTask = owner is not null
            ? dialog.ShowDialog<bool>(owner)
            : ShowOwnerlessAsync(dialog);

        resultTask.ContinueWith(
            t =>
            {
                result = t is { IsCompletedSuccessfully: true, Result: true };
                frame.Continue = false;
            },
            TaskScheduler.FromCurrentSynchronizationContext());

        Dispatcher.UIThread.PushFrame(frame);
        return result;
    }

    // No owner window yet (e.g. a startup crash): show it non-modally and complete when it closes.
    private static Task<bool> ShowOwnerlessAsync(ConfirmWindow dialog)
    {
        var tcs = new TaskCompletionSource<bool>();
        dialog.Closed += (_, _) => tcs.TrySetResult(dialog.Result);
        dialog.Show();
        return tcs.Task;
    }
}
