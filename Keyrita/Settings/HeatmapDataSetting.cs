using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using Keyrita.Gui;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Settings
{
    /// <summary>
    /// Enumerates each kind of heat map the user can choose to display on the keyboard.
    /// </summary>
    public enum eHeatMap
    {
        [UIData("None")]
        None,

        [UIData("Character Frequency")]
        CharacterFrequency,

        [UIData("Bigram Frequency")]
        BigramFrequency,

        [UIData("Trigram Frequency")]
        TrigramFrequency
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
        }

        protected override void ChangeLimits()
        {
            mHeatMapData.Clear();
            mValueHasChanged = true;

            string allCharacters = SettingState.MeasurementSettings.CharFrequencyData.UsedCharset;
            if(allCharacters != null)
            {
                switch (SettingState.KeyboardSettings.DisplayedHeatMap.Value)
                {
                    case eHeatMap.CharacterFrequency:
                        // Compute the heatmap value for each key.
                        LoadCharFrequencyHeatMap(allCharacters);
                        break;
                    case eHeatMap.BigramFrequency:
                        LoadBigramFrequencyHeatMap(allCharacters);
                        break;
                    case eHeatMap.TrigramFrequency:
                        break;
                }
            }
        }

        protected void LoadCharFrequencyHeatMap(string allCharacters)
        {
            var kfd = SettingState.MeasurementSettings.CharFrequencyData;

            double maxCharFrequency = kfd.MaxCharFreq;

            for(int i = 0; i < allCharacters.Length; i++)
            {
                mHeatMapData[allCharacters[i]] = kfd.GetCharFreq(i) / maxCharFrequency;
            }
        }

        protected void LoadBigramFrequencyHeatMap(string allCharacters)
        {
            // If a character is involved in a bigram it will be added to the bigram heatmap data.
            var kfd = SettingState.MeasurementSettings.CharFrequencyData;
            double maxFreq = kfd.MaxBigramFreq;

            // All each character to the dictionary.
            for(int i = 0; i < allCharacters.Length; i++)
            {
                mHeatMapData.Add(allCharacters[i], 0);
            }

            for(int i = 0; i < allCharacters.Length; i++)
            {
                for(int j = 0; j < allCharacters.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var bigramFrequency = kfd.GetBigramFreq(i, j);

                    // Both characters should report that they were part of the bigram.
                    mHeatMapData[allCharacters[i]] += bigramFrequency;
                    mHeatMapData[allCharacters[j]] += bigramFrequency;
                }
            }

            double freqMax = 0;
            for(int i = 0; i < allCharacters.Length; i++)
            {
                if (mHeatMapData[allCharacters[i]] > freqMax)
                {
                    freqMax = HeatMapData[allCharacters[i]];
                }
            } 

            for(int i = 0; i < allCharacters.Length; i++)
            {
                mHeatMapData[allCharacters[i]] /= freqMax;
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
