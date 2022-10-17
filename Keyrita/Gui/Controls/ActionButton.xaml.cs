using System;
using System.Windows;
using System.Windows.Controls;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for ActionButton.xaml
    /// </summary>
    public partial class ActionButton : UserControlBase
    {
        public ActionButton()
        {
            InitializeComponent();
            this.mButton.Click += PerformAction;
            SyncWithSetting(this.mAction);
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
            button.SyncWithSetting((ActionSetting)e.NewValue);
        }

        protected void SyncWithSetting(ActionSetting newAction)
        {
            this.mAction = newAction;

            if (this.Action == null)
            {
                this.IsEnabled = false;
            }
            else
            {
                this.IsEnabled = true;
                this.mButton.Content = mAction.SettingName;
                this.mButton.ToolTip = mAction.ToolTip;
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

        protected override void OnClose()
        {
            Action = null;
        }
    }
}
