using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for ButtonMenuItem.xaml
    /// NOTE: this will not unregister ui notifications on close.
    /// IF I EVER ADD A MENU TO A NON MAIN WINDOW, THAT NEEDS TO BE FIXED.!!
    /// </summary>
    public partial class ButtonMenuItem : MenuItem
    {
        public ButtonMenuItem()
        {
            InitializeComponent();
            Click += MenuItemClicked;
            SyncWithSetting(this.mAction);
        }

        protected void MenuItemClicked(object sender, RoutedEventArgs e)
        {
            // Just open the dialog.
            Action.Trigger();
        }

        protected static readonly DependencyProperty ActionProperty = DependencyProperty.Register(nameof(Action),
            typeof(ActionSetting),
            typeof(ButtonMenuItem),
            new PropertyMetadata(OnSettingChanged));

        protected static void OnSettingChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            // Check for null.
            ButtonMenuItem dlg = (ButtonMenuItem)source;
            dlg.SyncWithSetting((ActionSetting)e.NewValue);
        }

        protected void SyncWithSetting(ActionSetting newAction)
        {
            this.mAction = (ActionSetting)newAction;

            if (this.Action == null)
            {
                this.IsEnabled = false;
            }
            else
            {
                this.IsEnabled = true;
                this.Header = this.mAction.SettingName;
                this.ToolTip = this.mAction.ToolTip;
            }
        }

        public ActionSetting Action
        {
            get
            {
                return mAction;
            }
            set
            {
                SetValue(ActionProperty, value);
            }
        }

        protected ActionSetting mAction;
    }
}
