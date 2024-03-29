﻿using Keyrita.Gui;
using Keyrita.Gui.Controls;
using Keyrita.Measurements;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Dynamic;
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

        public ElementSetSetting<char> AvailableCharSet { get; } =
            new CharacterSetSetting();

        public KeyboardStateSetting KeyboardState { get; } =
            new KeyboardStateSetting();

        public EnumValueSetting KeyboardDisplay { get; } =
            new KeyboardDisplaySetting();

        public OnOffSetting KeyboardValid { get; } =
            new KeyboardValidSetting();

        public OnOffSetting ShowFingerUsage { get; } =
            new ShowFingerUsage();

        public EnumValueSetting<eHeatMap> HeatmapType { get; } =
            new HeatmapSetting();

        public HeatmapDataSetting HeatmapData { get; } =
            new HeatmapDataSetting();

        public SelectedKeySetting SelectedKey { get; } =
            new SelectedKeySetting();

        public ScissorMapSetting ScissorMap { get; } =
            new ScissorMapSetting();

        public EnumValueSetting<eKeyboardEditMode> KeyboardEditMode { get; } =
            new KeyboardEditMode();

        public LockedKeysSetting LockedKeys { get; set; } =
            new LockedKeysSetting();
    }

    /// <summary>
    /// Settings related directly to keyboard measurement and analysis.
    /// </summary>
    public class MeasurementSettings
    {
        public EnumValueSetting<eFinger> SpaceFinger { get; } =
            new SpaceFingerSetting();

        public CharFrequencySetting CharFrequencyData { get; } =
            new CharFrequencySetting();

        public ActionSetting PerformAnalysis { get; } =
            new KeyboardAnalysisAction();

        public OnOffSetting ShowAnnotations { get; } =
            new KeyboardShowAnnotationsSetting();

        public OnOffSetting AnalysisEnabled { get; } =
            new AnalysisEnabledSetting();

        public ElementSetSetting<eMeasurements> AvailableMeasurements { get; } =
            new AvailableMeasurementList();

        public TrigramDepthSetting TrigramDepth { get; } =
            new TrigramDepthSetting();

        public SortedTrigramSetSetting SortedTrigramSet { get; } =
            new SortedTrigramSetSetting();

        public TrigramCoverageSetting TrigramCoverage { get; } =
            new TrigramCoverageSetting();

        public CharacterToTrigramSetSetting CharacterToTrigramSet { get; } =
            new CharacterToTrigramSetSetting();

        public IReadOnlyDictionary<int, ConcreteValueSetting<double>> RowOffsets => mRowOffsets;
        protected Dictionary<int, ConcreteValueSetting<double>> mRowOffsets = new Dictionary<int, ConcreteValueSetting<double>>();

        public IReadOnlyDictionary<eMeasurements, MeasurementInstalledSetting> InstalledPerFingerMeasurements => mInstalledMeasurements;
        protected Dictionary<eMeasurements, MeasurementInstalledSetting> mInstalledMeasurements = new Dictionary<eMeasurements, MeasurementInstalledSetting>(); 

        public IReadOnlyDictionary<eMeasurements, MeasurementInstalledSetting> InstalledDynamicMeasurements => mInstalledDynamicMeasurements;
        protected Dictionary<eMeasurements, MeasurementInstalledSetting> mInstalledDynamicMeasurements = new Dictionary<eMeasurements, MeasurementInstalledSetting>(); 

        public MeasurementSettings()
        {
            for (var i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                mRowOffsets[i] = new RowHorizontalOffsetSetting(i);
            }

            foreach(eMeasurements meas in MeasUtil.PerFingerMeasurements)
            {
                mInstalledMeasurements.Add(meas, new MeasurementInstalledSetting(meas));
            }

            foreach(eMeasurements meas in MeasUtil.DynamicMeasurements)
            {
                mInstalledDynamicMeasurements.Add(meas, new MeasurementInstalledSetting(meas));
            }
        }
    }

    /// <summary>
    /// Actions which can be done by a user.
    /// </summary>
    public class UserActions
    {
        public IReadOnlyDictionary<eKeyboardReflectDirection, ActionSetting> ReflectActions => mReflectActions;
        public IReadOnlyDictionary<eMeasurements, ActionSetting> AddMeasurements => mAddMeasurements;

        private Dictionary<eKeyboardReflectDirection, ActionSetting> mReflectActions =
            new Dictionary<eKeyboardReflectDirection, ActionSetting>();

        private Dictionary<eMeasurements, ActionSetting> mAddMeasurements =
            new Dictionary<eMeasurements, ActionSetting>();

        public UserActions()
        {
            foreach (var dir in
                     Utils.GetTokens<eKeyboardReflectDirection>())
            {
                mReflectActions[dir] = new UserActionReflect(dir);
            }

            foreach (var meas in
                     Utils.GetTokens<eMeasurements>())
            {
                mAddMeasurements[meas] = new AddMeasurementAction(meas);
            }
        }
    }

    public class FingerSettings
    {
        public FingerHomePositionSetting FingerHomePosition { get; } =
            new FingerHomePositionSetting();

        public KeyMappingSetting KeyMappings { get; } =
            new KeyMappingSetting();

        public FingerWeightsSetting FingerWeights { get; } =
            new FingerWeightsSetting();

        public EffortMapSetting EffortMap { get; } =
            new EffortMapSetting();
    }

    /// <summary>
    /// A static class handling settings.
    /// </summary>
    public class SettingState
    {
        public static KeyboardSettings KeyboardSettings { get; private set; }
        public static MeasurementSettings MeasurementSettings { get; private set; }
        public static UserActions UserActions { get; private set; }
        public static FingerSettings FingerSettings { get; private set; }

        /// <summary>
        /// Action setings which open each available dialog.
        /// </summary>
        public static IReadOnlyDictionary<eDlgId, OpenDlgSetting> OpenDialogSettings => mOpenDialogSettings;
        private static Dictionary<eDlgId, OpenDlgSetting> mOpenDialogSettings = new();

        /// <summary>
        /// Initializes the global settings.
        /// </summary>
        public static void Init()
        {
            LogUtils.LogInfo("Initializing setting system.");
            CreateSettings();
        }

        private static void CreateSettings()
        {
            KeyboardSettings = new KeyboardSettings();
            // Locked keys setting.

            // Settings for measurements.
            MeasurementSettings = new MeasurementSettings();
            // Active measurement list.
            // Selected measurement.

            // Create the settings which open dialogs.
            var allDialogs = Utils.GetTokens<eDlgId>();
            foreach (var dialog in allDialogs)
            {
                mOpenDialogSettings[dialog] = new OpenDlgSetting(dialog);
            }

            UserActions = new UserActions();

            FingerSettings = new FingerSettings();
        }
    }
}
