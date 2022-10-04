using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings
{
    public class KeyboardSettings : IKeyboardSettings
    {
        public IEnumValueSetting<eKeyboardShape> KeyboardShape { get; } =
            new KeyboardShapeSetting("Keyboard Shape");
    }

    /// <summary>
    /// A static class handling settings.
    /// </summary>
    public class SettingState
    {
        public static KeyboardSettings KeyboardSettings { get; private set; }

        /// <summary>
        /// Initializes the global settings.
        /// </summary>
        public static void Init()
        {
            LTrace.LogInfo("Initializing setting system.");
            CreateSettings();
        }

        private static void CreateSettings()
        {
            KeyboardSettings = new KeyboardSettings();

            // Finger default position setting.
            // Locked keys setting.
            // Bigram frequency setting.
            // Trigram frequency setting.
            // Most common words setting.
        }
    }
}
