using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using Keyrita.Gui;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Settings
{
    /// <summary>
    /// Enumerates each kind of heat map the user can choose to display on the keyboard.
    /// </summary>
    public enum eHeatMap
    {
        [UIData("None")]
        None,

        [UIData("Char Freq")]
        CharacterFrequency,

        [UIData("Relative Char Freq")]
        RelativeCharacterFrequency,

        [UIData("Bigram Freq")]
        BigramFrequency,
    }

    /// <summary>
    /// The key the user currently has selected. Used for
    /// visualizing where the key is on the keyboard and for heatmap data.
    /// </summary>
    public class SelectedKeySetting : ConcreteValueSetting<char>
    {
        public SelectedKeySetting() 
            : base("Selected Key", ' ', eSettingAttributes.None)
        {
        }

        protected override void SetDependencies()
        {
            SettingState.KeyboardSettings.AvailableCharSet.AddDependent(this);
        }

        public void SetSelection(char selection)
        {
            if (SettingState.KeyboardSettings.AvailableCharSet.Collection.Contains(selection))
            {
                mPendingValue = selection;
            }
            else
            {
                LTrace.Assert(false, "Attempted to select an invalid key");
                mPendingValue = ' ';
            }

            TrySetToPending();
        }

        protected override void ChangeLimits()
        {
            if (!SettingState.KeyboardSettings.AvailableCharSet.Collection.Contains(mValue))
            {
                mValue = ' ';
            }
        }
    }

    /// <summary>
    /// Computes the current heatmap values per key.
    /// </summary>
    public class HeatmapDataSetting : SettingBase
    {
        public override bool HasValue => mHeatMapData.Count > 0;
        protected override bool ValueHasChanged => mValueHasChanged;

        public IReadOnlyDictionary<char, double> HeatMapData => mHeatMapData;
        protected Dictionary<char, double> mHeatMapData = new Dictionary<char, double>();

        protected bool mValueHasChanged = false;

        public HeatmapDataSetting() 
            : base("Heatmap Value", eSettingAttributes.None)
        {
        }

        protected override void SetDependencies()
        {
            // Dependent on char frequencies, and the type of heatmap to display.
            SettingState.KeyboardSettings.DisplayedHeatMap.AddDependent(this);
            SettingState.MeasurementSettings.CharFrequencyData.AddDependent(this);
            SettingState.KeyboardSettings.KeyboardState.AddDependent(this);
            SettingState.KeyboardSettings.SelectedKey.AddDependent(this);
        }

        protected override void ChangeLimits()
        {
            mHeatMapData.Clear();
            mValueHasChanged = true;

            var usedKeys = new HashSet<char>();
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    usedKeys.Add(SettingState.KeyboardSettings.KeyboardState.GetValueAt(i, j));
                }
            }

            string allCharacters = SettingState.MeasurementSettings.CharFrequencyData.UsedCharset;
            char selectedKey = SettingState.KeyboardSettings.SelectedKey.Value;

            if(allCharacters != null)
            {
                switch (SettingState.KeyboardSettings.DisplayedHeatMap.Value)
                {
                    case eHeatMap.CharacterFrequency:
                        // Compute the heatmap value for each key.
                        selectedKey = ' ';
                        goto case eHeatMap.RelativeCharacterFrequency;
                    case eHeatMap.RelativeCharacterFrequency:
                        LoadCharFrequencyHeatMap(allCharacters, usedKeys, selectedKey);
                        break;
                    case eHeatMap.BigramFrequency:
                        LoadBigramFrequencyHeatMap(allCharacters, usedKeys, selectedKey);
                        break;
                }
            }
        }

        protected void LoadCharFrequencyHeatMap(string allCharacters, HashSet<char> usedKeys, char selectedKey)
        {
            var kfd = SettingState.MeasurementSettings.CharFrequencyData;
            double maxCharFrequency = -1;

            if (usedKeys.Contains(selectedKey))
            {
                if (!allCharacters.Contains(selectedKey))
                {
                    LTrace.Assert(false, "Used keys should be a subset of all characters");
                }
                else
                {
                    maxCharFrequency = kfd.GetCharFreq(allCharacters.IndexOf(selectedKey));
                }
            }
            else
            {
                for(int i = 0; i < allCharacters.Length; i++)
                {
                    if (usedKeys.Contains(allCharacters[i]))
                    {
                        double freq = kfd.GetCharFreq(i);
                        if(freq > maxCharFrequency)
                        {
                            maxCharFrequency = freq;
                        }
                    }
                }
            }

            for(int i = 0; i < allCharacters.Length; i++)
            {
                mHeatMapData[allCharacters[i]] = Math.Min(1, kfd.GetCharFreq(i) / maxCharFrequency);
            }
        }

        protected void LoadBigramFrequencyHeatMap(string allCharacters, HashSet<char> usedKeys, char selectedKey)
        {
            var kfd = SettingState.MeasurementSettings.CharFrequencyData;
            double maxFreq = -1;

            if (allCharacters.Contains(selectedKey) && usedKeys.Contains(selectedKey))
            {
                int selectedKeyIdx = allCharacters.IndexOf(selectedKey);

                for(int i = 0; i < allCharacters.Length; i++)
                {
                    if (usedKeys.Contains(allCharacters[i]))
                    {
                        double freq = kfd.GetBigramFreq(selectedKeyIdx, i);
                        if(freq > maxFreq)
                        {
                            maxFreq = freq;
                        }
                    }
                }

                for(int i = 0; i < allCharacters.Length; i++)
                {
                    mHeatMapData[allCharacters[i]] = Math.Min(1, kfd.GetBigramFreq(selectedKeyIdx, i) / maxFreq);
                }
            }
        }

        public override void SetToDefault()
        {
            // No default value, purely derived state.
        }

        protected override void Action()
        {
        }

        protected override void Save(XmlWriter writer)
        {
        }

        protected override void Load(string text)
        {
        }

        public override void SetToDesiredValue()
        {
        }

        protected override void SetToNewLimits()
        {
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            if(ValueHasChanged)
            {
                SettingTransaction("Heatmap updating", false, () =>
                {
                    mValueHasChanged = false;
                });
            }
        }
    }
}
