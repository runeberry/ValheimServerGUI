using System.Collections.Generic;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>Records confirmation prompts and returns a preset answer.</summary>
internal sealed class RecordingUserPrompt : IUserPrompt
{
    private readonly bool _answer;

    public RecordingUserPrompt(bool answer) => _answer = answer;

    public List<(string Message, string Title)> Prompts { get; } = new();

    public bool Confirm(string message, string title)
    {
        Prompts.Add((message, title));
        return _answer;
    }
}
