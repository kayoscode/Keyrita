using Keyrita.Util;
using Keyrita.Gui.Dialogs;
using System.Windows;

namespace Keyrita.Gui
{
    /// <summary>
    /// The individual dialogs which can appear in the program.
    /// </summary>
    public enum eDlgId
    {
        // UIData is the name of the window in this context.
        [UIData("Settings")]
        SettingsDlg
    }

    /// <summary>
    /// Produces a dialog based on the input dialog id.
    /// </summary>
    public static class DialogFactory
    {
        public static Window GetDialogWindow(eDlgId dlg)
        {
            switch (dlg)
            {
                case eDlgId.SettingsDlg:
                    return new SettingsDialog();
                default:
                    LTrace.Assert(false, "Unsupported dialog");
                    return null;
            }
        }
    }
}
