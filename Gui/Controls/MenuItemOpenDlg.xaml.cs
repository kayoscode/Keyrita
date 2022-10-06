using Keyrita.Settings.SettingUtil;
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

        protected DependencyProperty DialogProperty = DependencyProperty.Register(nameof(Dialog),
            typeof(eDlgId),
            typeof(MenuItemOpenDlg));

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
