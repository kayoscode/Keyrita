using Keyrita.Settings;
using Keyrita.Util;
using Microsoft.Win32;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for MenuItemOpenDlg.xaml
    /// </summary>
    public partial class MenuItemOpenDlg : MenuItem
    {
        public MenuItemOpenDlg()
        {
            InitializeComponent();

            Click += MenuItemClicked;
            SyncWithDialog(this.Dialog);
        }

        protected void MenuItemClicked(object sender, RoutedEventArgs e)
        {
            // Just open the dialog.
            SettingState.OpenDialogSettings[Dialog].OpenDialog();
        }

        protected static readonly DependencyProperty DialogProperty = DependencyProperty.Register(nameof(Dialog),
            typeof(eDlgId),
            typeof(MenuItemOpenDlg),
            new PropertyMetadata(OnSettingChanged));

        protected static void OnSettingChanged(DependencyObject source,
                                               DependencyPropertyChangedEventArgs e)
        {
            MenuItemOpenDlg dlg = (MenuItemOpenDlg)source;
            dlg.SyncWithDialog((eDlgId)e.NewValue);
        }

        public void SyncWithDialog(eDlgId dlg)
        {
            mDialog = dlg;
            Header = dlg.UIText();
            ToolTip = dlg.UIToolTip();
        }

        public eDlgId Dialog
        {
            get
            {
                return mDialog;
            }
            set
            {
                SetValue(DialogProperty, value);
            }
        }

        protected eDlgId mDialog;
    }
}
