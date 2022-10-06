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
            mKeyboardDisplay.Setting = KeyboardDisplaySetting;
            mKeyboardShape.Setting = KeyboardShapeSetting;
            mLanguage.Setting = LanguageSetting;

            this.ResizeMode = System.Windows.ResizeMode.NoResize;
        }

        private EnumValueSetting KeyboardDisplaySetting =>
            SettingState.KeyboardSettings.KeyboardDisplay as EnumValueSetting;

        private EnumValueSetting KeyboardShapeSetting =>
            SettingState.KeyboardSettings.KeyboardShape as EnumValueSetting;

        private EnumValueSetting LanguageSetting =>
            SettingState.KeyboardSettings.KeyboardLanguage as EnumValueSetting;
    }
}
