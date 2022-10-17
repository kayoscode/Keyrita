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
        [UIData("Settings", "Opens the main settings dialog")]
        SettingsDlg,
        [UIData("Swap Characters", "Opens a dialog which allows you to swap characters with others in the available set.")]
        SetCharactersDlg
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
                case eDlgId.SetCharactersDlg:
                    return new AvailableChars();
                default:
                    LTrace.Assert(false, "Unsupported dialog");
                    return null;
            }
        }
    }
}
