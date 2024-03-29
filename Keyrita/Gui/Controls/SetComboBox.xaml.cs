﻿using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for DropdownList.xaml
    /// </summary>
    public partial class SetComboBox : UserControlBase
    {
        public SetComboBox()
        {
            InitializeComponent();
        }

        private void SettingUpdated(object changedSetting)
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
                item.ToolTip = Setting.GetToolTipForToken(token);
                item.HorizontalAlignment = this.HorizontalAlignment;
                item.HorizontalContentAlignment = this.HorizontalAlignment;
                item.VerticalAlignment = this.VerticalAlignment;
                item.VerticalContentAlignment = this.VerticalAlignment;
                mComboBox.Items.Add(item);
            }

            mComboBox.IsEnabled = Setting.ValidTokens.Count > 1;
            this.ToolTip = Setting.ToolTip;

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
                mSetting.ValueChangedNotifications.Add(SettingUpdated);
                mSetting.LimitsChangedNotifications.Add(SettingUpdated);

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

        protected override void OnClose()
        {
            Setting = null;
        }
    }
}
