using Keyrita.Settings;
using Keyrita.Util;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for DropdownList.xaml
    /// </summary>
    public partial class DropdownList : UserControl
    {
        public DropdownList()
        {
            InitializeComponent();
            Setting = SettingState.KeyboardSettings.KeyboardShape as EnumValueSetting;

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

            SetSelection();
        }

        private void SetSelection()
        {
            if(Setting.HasValue)
            {
                mComboBox.SelectedIndex = Setting.GetIndexOfSelection();
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

        public EnumValueSetting Setting;
    }
}
