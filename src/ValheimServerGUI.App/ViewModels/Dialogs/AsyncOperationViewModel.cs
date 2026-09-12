using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Generic async-operation dialog VM (§11): runs a <see cref="Task"/> behind a modal with in-progress →
/// success/failure states. Used for report submission.
/// </summary>
public partial class AsyncOperationViewModel : ObservableObject
{
    public enum OpState { Running, Success, Failure }

    private readonly string _successMessage;
    private readonly string _failureMessage;

    public AsyncOperationViewModel(string runningMessage, string successMessage, string failureMessage)
    {
        _message = runningMessage;
        _successMessage = successMessage;
        _failureMessage = failureMessage;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning), nameof(IsDone))]
    private OpState _state = OpState.Running;

    [ObservableProperty] private string _message;

    public bool IsRunning => State == OpState.Running;
    public bool IsDone => State != OpState.Running;

    public async Task RunAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            State = OpState.Success;
            Message = _successMessage;
        }
        catch (Exception ex)
        {
            State = OpState.Failure;
            Message = $"{_failureMessage}: {ex.Message}";
        }
    }
}
