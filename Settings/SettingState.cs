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
        public IEnumValueSetting KeyboardShape { get; } =
            new KeyboardShapeSetting();

        public IEnumValueSetting KeyboardLanguage { get; } =
            new KeyboardLanguageSetting();
    }

    public class MeasurementSettings : IMeasurementSettings
    {
        public IOnOffSetting ShowAnnotations { get; } =
            new KeyboardShowAnnotationsSetting();
    }

    /// <summary>
    /// A static class handling settings.
    /// </summary>
    public class SettingState
    {
        public static KeyboardSettings KeyboardSettings { get; private set; }
        public static MeasurementSettings MeasurementSettings { get; private set; }

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

            // Settings for measurements.
            MeasurementSettings = new MeasurementSettings();
            // Active measurement list.
            // Selected measurement.
        }
    }
}
