using Keyrita.Util;
using System;
using System.Windows;

namespace Keyrita.Gui.Dialogs
{
    public partial class WindowBase : Window
    {
        protected override void OnClosed(EventArgs e)
        {
            LTrace.LogInfo($"Closing window {this.Title}");
            base.OnClosed(e);
        }
    }
}
