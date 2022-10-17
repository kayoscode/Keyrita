using Keyrita.Settings;

namespace Keyrita.Gui.Dialogs
{
    /// <summary>
    /// Interaction logic for AvailableChars.xaml
    /// </summary>
    public partial class AvailableChars : WindowBase
    {
        public AvailableChars()
        {
            InitializeComponent();
            mAvailableChars.KeyboardState = SettingState.KeyboardSettings.KeyboardState;
            mAvailableChars.AvailableChars = SettingState.KeyboardSettings.AvailableCharSet;
        }
    }
}
