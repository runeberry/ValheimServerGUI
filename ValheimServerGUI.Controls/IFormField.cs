namespace ValheimServerGUI
{
    public interface IFormField
    {
        public string LabelText { get; set; }

        public string HelpText { get; set; }
    }

    public interface IFormField<T> : IFormField
    {
        public T Value { get; set; }
    }
}
