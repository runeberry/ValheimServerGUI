using System.Collections.Generic;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>Recording user prompt: returns a configured answer and logs every confirm call.</summary>
    public class FakeUserPrompt : IUserPrompt
    {
        private readonly bool _answer;

        public FakeUserPrompt(bool answer = false)
        {
            _answer = answer;
        }

        public List<(string Message, string Title)> Confirmations { get; } = new();

        public bool Confirm(string message, string title)
        {
            Confirmations.Add((message, title));
            return _answer;
        }
    }
}
