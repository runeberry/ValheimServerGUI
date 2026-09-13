using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// The app's editable list field (the WinForms <c>AddRemoveListField</c> equivalent): a
/// <see cref="SelectListField"/> plus Add / Edit / Remove icon buttons. Thin and MVVM-idiomatic — the
/// ViewModel owns the items (<see cref="ItemsSource"/>) and selection (<see cref="Value"/>), and supplies the
/// three <see cref="System.Windows.Input.ICommand"/>s; each button has its own independent enable flag (the
/// WinForms version had a copy-paste bug where Edit/Remove both read the Add flag — not reproduced here).
/// </summary>
/// <remarks>Design (markup) lives in <c>AddRemoveListField.axaml</c>; functional code in <c>AddRemoveListField.cs</c>.</remarks>
public partial class AddRemoveListField : UserControl
{
    public AddRemoveListField() => AvaloniaXamlLoader.Load(this);
}
