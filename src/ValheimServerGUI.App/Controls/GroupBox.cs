using Avalonia.Controls.Primitives;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// A classic etched WinForms-style titled group box: a thin inset frame whose title text sits on the
/// top-left edge, notching the border line. <see cref="HeaderedContentControl.Header"/> is the title and
/// <see cref="Avalonia.Controls.ContentControl.Content"/> is the body. The look lives in
/// <c>GroupBox.axaml</c>; the notch background is the control's <c>Background</c> so it can be matched to
/// whatever surface the box sits on.
/// </summary>
public class GroupBox : HeaderedContentControl
{
}
