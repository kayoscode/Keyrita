using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for ButtonMenuItem.xaml
    /// </summary>
    public partial class ButtonMenuItem : MenuItem
    {
        public ButtonMenuItem()
        {
            InitializeComponent();

            Click += MenuItemClicked;
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
            dlg.mAction = (ActionSetting)e.NewValue;

            if (dlg.Action == null)
            {
                dlg.IsEnabled = false;
            }
            else
            {
                dlg.IsEnabled = true;
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
