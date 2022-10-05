﻿using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings
{
    /// <summary>
    /// Settings related to the state of the keyboard.
    /// </summary>
    public class KeyboardSettings
    {
        public EnumValueSetting KeyboardShape { get; } =
            new KeyboardShapeSetting();

        public EnumValueSetting KeyboardLanguage { get; } =
            new KeyboardLanguageSetting();

        public ElementSetSetting AvailableCharSet { get; } =
            new CharacterSetSetting();

        public KeyboardStateSetting KeyboardState { get; } =
            new KeyboardStateSetting();
    }

    /// <summary>
    /// Settings related directly to keyboard measurement and analysis.
    /// </summary>
    public class MeasurementSettings
    {
        public OnOffSetting ShowAnnotations { get; } =
            new KeyboardShowAnnotationsSetting();

        public IReadOnlyDictionary<int, ConcreteValueSetting<double>> RowOffsets => mRowOffsets;
        protected Dictionary<int, ConcreteValueSetting<double>> mRowOffsets = new Dictionary<int, ConcreteValueSetting<double>>();

        public MeasurementSettings()
        {
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                mRowOffsets[i] = new RowHorizontalOffsetSetting(i);
            }
        }
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
