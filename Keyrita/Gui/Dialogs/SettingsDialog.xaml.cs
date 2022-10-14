using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Dialogs
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsDialog : WindowBase
    {
        public SettingsDialog()
        {
            InitializeComponent();

            mKeyboardDisplay.Setting = SettingState.KeyboardSettings.KeyboardDisplay as EnumValueSetting;
            mKeyboardShape.Setting = SettingState.KeyboardSettings.KeyboardShape as EnumValueSetting;
            mLanguage.Setting = SettingState.KeyboardSettings.KeyboardLanguage as EnumValueSetting;
            mShowFingerUsage.Setting = SettingState.KeyboardSettings.ShowFingerUsage as OnOffSetting;
            mHeatMapSetting.Setting = SettingState.KeyboardSettings.DisplayedHeatMap as EnumValueSetting;

            this.ResizeMode = System.Windows.ResizeMode.NoResize;
        }
    }
}
