using System.Windows;
using System.Windows.Controls;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for ActionButton.xaml
    /// </summary>
    public partial class ActionButton : UserControl
    {
        public ActionButton()
        {
            InitializeComponent();
            this.mButton.Click += PerformAction;
        }

        protected void PerformAction(object sender, RoutedEventArgs e)
        {
            // Just open the dialog.
            Action.Trigger();
        }

        protected static readonly DependencyProperty ActionProperty = DependencyProperty.Register(nameof(Action),
            typeof(ActionSetting),
            typeof(ActionButton),
            new PropertyMetadata(OnSettingChanged));

        protected static void OnSettingChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            // Check for null.
            ActionButton button = (ActionButton)source;
            button.mAction = (ActionSetting)e.NewValue;

            if (button.Action == null)
            {
                button.IsEnabled = false;
            }
            else
            {
                button.IsEnabled = true;
                button.mButton.Content = button.mAction.SettingName;
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
