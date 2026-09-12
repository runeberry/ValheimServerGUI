namespace ValheimServerGUI.App.Startup;

/// <summary>Progress update reported by <see cref="StartupService"/> to the splash view.</summary>
/// <param name="Message">Human-readable status line.</param>
/// <param name="Fraction">Completion in [0, 1].</param>
public readonly record struct StartupProgress(string Message, double Fraction);
