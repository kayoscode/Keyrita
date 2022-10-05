using Keyrita.Settings;
using System.Windows;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            mKeyboardShape.Setting = KeyboardShapeSetting;
            mLanguage.Setting = LanguageSetting;
            mShowAnnotations.Setting = ShowAnnotationsSetting;

            mKeyboardControl.KeyboardState = SettingState.KeyboardSettings.KeyboardState;
        }

        private EnumValueSetting KeyboardShapeSetting =>
            SettingState.KeyboardSettings.KeyboardShape as EnumValueSetting;

        private EnumValueSetting LanguageSetting =>
            SettingState.KeyboardSettings.KeyboardLanguage as EnumValueSetting;
        
        private OnOffSetting ShowAnnotationsSetting =>
            SettingState.MeasurementSettings.ShowAnnotations as OnOffSetting;
    }
}
