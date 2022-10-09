using Keyrita.Settings;
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
            dlg.Dialog = (eDlgId)e.NewValue;
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
