using Keyrita.Settings;
using Keyrita.Util;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for DropdownList.xaml
    /// </summary>
    public partial class SetComboBox : UserControl
    {
        public SetComboBox()
        {
            InitializeComponent();
        }

        private void SettingUpdated(SettingBase changedSetting)
        {
            SyncWithSetting();
        }

        private void SyncWithSetting()
        {
            mComboBox.Items.Clear();

            foreach (Enum token in Setting.ValidTokens)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = token.UIText();
                mComboBox.Items.Add(item);
            }

            mComboBox.IsEnabled = Setting.ValidTokens.Count > 1;

            SetSelection();
        }

        private void SetSelection()
        {
            if(Setting.HasValue)
            {
                mComboBox.SelectionChanged -= ComboBox_SelectionChanged;
                mComboBox.SelectedIndex = Setting.GetIndexOfSelection();
                mComboBox.SelectionChanged += ComboBox_SelectionChanged;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update setting value based on new selection.
            if(e.AddedItems.Count > 0)
            {
                int index = mComboBox.SelectedIndex;
                Enum element = Setting.GetTokenAtIndex(index);
                Setting.Set(element);
            }
        }

        private static readonly DependencyProperty SettingProperty =
            DependencyProperty.Register(nameof(Setting),
                                        typeof(EnumValueSetting),
                                        typeof(SetComboBox),
                                        new PropertyMetadata(OnSettingChanged));

        private static void OnSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            SetComboBox control = source as SetComboBox;
            control.UpdateSetting(e.NewValue as EnumValueSetting);
        }

        private void UpdateSetting(EnumValueSetting newValue)
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
        public EnumValueSetting Setting
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

        private EnumValueSetting mSetting;
    }
}
