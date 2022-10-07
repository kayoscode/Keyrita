using Keyrita.Gui;
using Keyrita.Util;
using System;
using System.Windows;

namespace Keyrita.Settings.SettingUtil
{
    /// <summary>
    /// Setting to handle opening and closing dialogs.
    /// Sets the window title and enforces that only one can be opened at a single time.
    /// </summary>
    public class OpenDlgSetting : ConcreteValueSetting<Window>
    {
        protected eDlgId Dialog { get; }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="dialog"></param>
        public OpenDlgSetting(eDlgId dialog) 
            : base($"Dialog {dialog.UIText()} Open", null, eSettingAttributes.None)
        {
            this.Dialog = dialog;
        }

        /// <summary>
        /// Tells the setting to open the dialog and updates the state.
        /// </summary>
        public void OpenDialog()
        {
            if(Value == null)
            {
                mPendingValue = DialogFactory.GetDialogWindow(Dialog);
                TrySetToPending();
            }
            else
            {
                var window = (Window)Value;
                window.Focus();
            }
        }

        /// <summary>
        /// Informs the setting that the dialog has been closed.
        /// </summary>
        protected void CloseDialog()
        {
            if(Value != null)
            {
                mPendingValue = null;
                TrySetToPending();
            }
        }

        protected override void Action()
        {
            // If we just set the value to true in the setting transaction, open the dialog.
            // Register an interest in it to close the dialog after the x button is clicked.
            var window = Value as Window;

            if(Value != null)
            {
                window.Title = Dialog.UIText();
                window.Closed += (object sender, EventArgs e) => CloseDialog();
                window.Show();
                window.Owner = Application.Current.MainWindow;
            }
        }
    }
}
