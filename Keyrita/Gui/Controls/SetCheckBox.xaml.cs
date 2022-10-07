using Keyrita.Settings.SettingUtil;
using System.Windows;
using System.Windows.Controls;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for SetCheckBox.xaml
    /// </summary>
    public partial class SetCheckBox : UserControl
    {
        public SetCheckBox()
        {
            InitializeComponent();
        }

        private void SettingUpdated(SettingBase changedSetting)
        {
            SyncWithSetting();
        }

        private void SyncWithSetting()
        {
            mCheckBox.Checked -= mCheckBox_Checked;
            mCheckBox.Unchecked -= mCheckBox_Unchecked;
            mCheckBox.IsChecked = Setting.IsOn;
            mCheckBox.Checked += mCheckBox_Checked;
            mCheckBox.Unchecked += mCheckBox_Unchecked;

            mCheckBox.IsEnabled = Setting.ValidTokens.Count > 1;
        }

        private void mCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            mSetting.Set(eOnOff.On);
        }

        private void mCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            mSetting.Set(eOnOff.Off);
        }

        private static readonly DependencyProperty SettingProperty =
            DependencyProperty.Register(nameof(Setting),
                                        typeof(OnOffSetting),
                                        typeof(SetCheckBox),
                                        new PropertyMetadata(OnSettingChanged));

        private static void OnSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as SetCheckBox;
            control.UpdateSetting(e.NewValue as OnOffSetting);
        }

        private void UpdateSetting(OnOffSetting newValue)
        {
            // Unregister setting change listeners.
            if (mSetting != null)
            {
                mSetting.ValueChangedNotifications.Remove(SettingUpdated);
                mSetting.LimitsChangedNotifications.Remove(SettingUpdated);
            }

            mSetting = newValue;

            if(mSetting != null)
            {
                mSetting.ValueChangedNotifications.AddGui(SettingUpdated);
                mSetting.LimitsChangedNotifications.AddGui(SettingUpdated);

                mSettingName.Text = Setting.SettingName;
                SyncWithSetting();
            }
        }

        /// <summary>
        /// The setting which this control is linked to.
        /// </summary>
        public OnOffSetting Setting
        {
            get
            {
                return mSetting;
            }
            set
            {
                SetValue(SettingProperty, value);
            }
        }

        private OnOffSetting mSetting;
    }
}
