using System.Windows.Forms;
using System.Windows.Interop;

namespace Vitrine.Engine.Panel;

/// <summary>
/// Bridges the WinForms Application.Run() message loop with WPF's input system.
/// WPF's TextCompositionManager (which translates WM_CHAR into TextInput events)
/// subscribes to ComponentDispatcher.ThreadFilterMessage / ThreadPreprocessMessage.
/// When hosting WPF windows in a WinForms app, these events are never raised
/// because the WinForms message loop doesn't call ComponentDispatcher.RaiseThreadMessage.
/// Result: keyboard text input is broken in WPF TextBoxes (typing shows nothing,
/// but Ctrl+V via command bindings still works because commands use a different path).
/// This filter forwards every WinForms message to ComponentDispatcher so WPF can
/// process it.
/// </summary>
internal class WpfMessageFilter : IMessageFilter
{
    public bool PreFilterMessage(ref Message m)
    {
        var msg = new MSG
        {
            hwnd = m.HWnd,
            message = m.Msg,
            wParam = m.WParam,
            lParam = m.LParam,
        };
        ComponentDispatcher.RaiseThreadMessage(ref msg);
        return false; // don't consume the message, let normal dispatch continue
    }
}
