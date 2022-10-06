
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for DropdownList.xaml
    /// </summary>
    public partial class SetRadioButton : UserControl
    {
        public SetRadioButton()
        {
            InitializeComponent();
        }

        private void SettingUpdated(SettingBase changedSetting)
        {
            SyncWithSetting();
        }

        private void SyncWithSetting()
        {
            mRadioButton.Children.Clear();

            foreach (Enum token in Setting.ValidTokens)
            {
                RadioButton item = new RadioButton();
                item.GroupName = mSetting.SettingName;
                item.Content = token.UIText();
                item.Checked += mRadioButton_SelectionChanged;

                item.HorizontalAlignment = HorizontalAlignment.Stretch;
                item.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                item.VerticalAlignment = VerticalAlignment.Center;
                item.VerticalContentAlignment = VerticalAlignment.Center;

                mRadioButton.Children.Add(item);
            }

            mRadioButton.IsEnabled = Setting.ValidTokens.Count > 1;

            SetSelection();
        }

        private void SetSelection()
        {
            int selectionIndex = Setting.GetIndexOfSelection();

            for(int i = 0; i < mRadioButton.Children.Count; i++)
            {
                if(i == selectionIndex)
                {
                    var rb = (RadioButton)mRadioButton.Children[i];
                    rb.Checked -= mRadioButton_SelectionChanged;
                    rb.IsChecked = true;
                    rb.Checked += mRadioButton_SelectionChanged;
                }
            }
        }

        private void mRadioButton_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Update setting value based on new selection.
            for (int i = 0; i < mRadioButton.Children.Count; i++)
            {
                RadioButton rb = (RadioButton)mRadioButton.Children[i];

                if (rb != null && rb.IsChecked.HasValue && rb.IsChecked.Value)
                {
                    Enum element = Setting.GetTokenAtIndex(i);
                    Setting.Set(element);
                    break;
                }
            }
        }

        private static readonly DependencyProperty SettingProperty =
            DependencyProperty.Register(nameof(Setting),
                                        typeof(EnumValueSetting),
                                        typeof(SetRadioButton),
                                        new PropertyMetadata(OnSettingChanged));

        private static void OnSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            SetRadioButton control = source as SetRadioButton;
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
