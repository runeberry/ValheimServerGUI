using System;

namespace ValheimServerGUI
{
    public interface IFormField
    {
        string LabelText { get; set; }

        string HelpText { get; set; }
    }

    public interface IFormField<T> : IFormField
    {
        T Value { get; set; }

        event EventHandler<T> ValueChanged;
    }
}
