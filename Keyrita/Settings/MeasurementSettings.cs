using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Xml;
using Keyrita.Measurements;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Settings
{
    /// <summary>
    /// Which finger will be used for the space key
    /// </summary>
    public class SpaceFingerSetting : EnumValueSetting<eFinger>
    {
        public SpaceFingerSetting() : base("Space Finger", eFinger.RightThumb, eSettingAttributes.Recall)
        {
            ModifyLimits();
        }

        protected override void ModifyLimits()
        {
            mValidTokens.Clear();
            mValidTokens.Add(eFinger.LeftThumb);
            mValidTokens.Add(eFinger.RightThumb);
        }
    }

    /// The set of measurements available to the user.
    /// Nonrecallable.
    /// </summary>
    public class AvailableMeasurementList : ElementSetSetting<eMeasurements>
    {
        public AvailableMeasurementList() 
            : base("Available Measurements", eSettingAttributes.None)
        {
            mDefaultCollection = Utils.GetTokens<eMeasurements>().ToHashSet<eMeasurements>();
        }
    }

    /// <summary>
    /// Action which adds a measurement to the system.
    /// </summary>
    public class AddMeasurementAction : ActionSetting
    {
        protected eMeasurements Meas => (eMeasurements)this.SInstance;

        public override string ToolTip
        {
            get
            {
                return this.SInstance.UIToolTip();
            }
        }

        public AddMeasurementAction(eMeasurements measurement)
            : base(measurement.UIText(), measurement)
        {
        }

        protected override void DoAction()
        {
            if (SettingState.MeasurementSettings.InstalledPerFingerMeasurements.ContainsKey(Meas))
            {
                SettingState.MeasurementSettings.InstalledPerFingerMeasurements[Meas].TurnMeasOn();
            }
            else if (SettingState.MeasurementSettings.InstalledDynamicMeasurements.ContainsKey(Meas))
            {
                SettingState.MeasurementSettings.InstalledDynamicMeasurements[Meas].TurnMeasOn();
            }
            else
            {
                LogUtils.Assert(false, "Unknown measurement type.");
            }
        }
    }

    /// <summary>
    /// On if the measurement is installed, off otherwise.
    /// </summary>
    public class MeasurementInstalledSetting : OnOffSetting
    {
        public MeasurementInstalledSetting(eMeasurements measurement)
            : base($"Measurement OnOff State", eOnOff.On, eSettingAttributes.None, measurement)
        {
        }

        protected override void Action()
        {
            if(this.IsOn)
            {
                AnalysisGraphSystem.InstallNode(this.SInstance);
            }
            else
            {
                AnalysisGraphSystem.RemoveNode(this.SInstance);
            }
        }

        public void TurnMeasOn()
        {
            this.PendingValue = eOnOff.On;
            this.TrySetToPending(true);
        }

        public void TurnMeasOff()
        {
            this.PendingValue = eOnOff.Off;
            this.TrySetToPending(true);
        }
    }

    /// <summary>
    /// The number of trigrams to use for computations.
    /// </summary>
    public class TrigramDepthSetting : ConcreteValueSetting<int>
    {
        public TrigramDepthSetting() : 
            base("Trigram Depth", 1000, eSettingAttributes.None)
        {
        }
    }

    public class SortedTrigramSetSetting : SettingBase
    {
        public SortedTrigramSetSetting() 
            : base("Most Significant Trigrams", eSettingAttributes.None)
        {
        }

        public override bool HasValue => true;

        protected override bool ValueHasChanged => mValueHasChanged;
        protected bool mValueHasChanged = false;
        public IReadOnlyList<byte[]> SortedTrigramSet => mMostSignificantTrigrams;
        protected List<byte[]> mMostSignificantTrigrams = new List<byte[]>();

        protected override void Init()
        {
            SettingState.MeasurementSettings.TrigramDepth.AddDependent(this);
            SettingState.MeasurementSettings.CharFrequencyData.AddDependent(this);
        }

        protected override void ModifyLimits()
        {
            mValueHasChanged = true;

            if(SettingState.MeasurementSettings.CharFrequencyData.HasValue && SettingState.MeasurementSettings.TrigramDepth.HasValue)
            {
                int tgCount = SettingState.MeasurementSettings.TrigramDepth.Value;

                LogUtils.Assert(SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.Length <= byte.MaxValue);
                this.mMostSignificantTrigrams.Clear();

                // Go through every trigram and setup append to each list the correct values.
                for(int i = 0; i < SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.Length; i++)
                {
                    for (int j = 0; j < SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[i].Length; j++)
                    {
                        for (int k = 0; k < SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[i].Length; k++)
                        {
                            byte[] trigram = new byte[3];
                            trigram[0] = (byte)i;
                            trigram[1] = (byte)j;
                            trigram[2] = (byte)k;

                            long trigramFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[i][j][k];

                            mMostSignificantTrigrams.Add(trigram);
                        }
                    }
                }

                // Sort them, then remove the unnecessary ones.
                mMostSignificantTrigrams.Sort(new Comparison<byte[]>((a, b) =>
                {
                    long aFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[a[0]][a[1]][a[2]];
                    long bFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[b[0]][b[1]][b[2]];

                    if (aFreq == bFreq) return 0;
                    return aFreq > bFreq ? -1 : 1;
                }));

                // Remove the unnecessary trigrams.
                mMostSignificantTrigrams.RemoveRange(tgCount, mMostSignificantTrigrams.Count() - tgCount);
            }
        }

        protected override void SetToNewLimits()
        {
            TrySetToPending(false);
        }

        public override void SetToDefault()
        {
        }

        public override void SetToDesiredValue()
        {
        }

        protected override void Action()
        {
        }

        protected override void Load(string text)
        {
        }

        protected override void Save(XmlWriter writer)
        {
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            if (ValueHasChanged)
            {
                string description = $"Adjusted most significant trigrams";

                InitiateSettingChange(description, userInitiated, () =>
                {
                    mValueHasChanged = false;
                });
            }
        }
    }

    public class CharacterToTrigramSetSetting : SettingBase
    {
        public CharacterToTrigramSetSetting() 
            : base("Character To Trigram Set", eSettingAttributes.None)
        {
        }

        public override bool HasValue => true;

        protected override bool ValueHasChanged => mValueHasChanged;
        protected bool mValueHasChanged = false;

        /// <summary>
        /// Stores a tuple with the following information.
        /// 1. The index in the byte array this character resides.
        /// 2. The trigramFrequency.
        /// 3. The three characters representing the trigram. Do not mutate.
        /// 
        /// Maps any two characters to the set of trigrams they are both involved in.
        /// </summary>
        public List<byte[]>[][] BothInvolvedTrigrams { get; set; } 

        /// <summary>
        /// Maps two characters to the set of trigrams that includes the first, but not the second character.
        /// </summary>
        public List<byte[]>[][] FirstInvolvedTrigrams { get; set; }

        protected override void Init()
        {
            SettingState.MeasurementSettings.SortedTrigramSet.AddDependent(this);
            SettingState.MeasurementSettings.CharFrequencyData.AddDependent(this);
        }

        protected override void ModifyLimits()
        {
            if (SettingState.MeasurementSettings.CharFrequencyData.HasValue && 
                SettingState.MeasurementSettings.SortedTrigramSet.HasValue)
            {
                mValueHasChanged = true;
                List<byte[]>[] charToTrigram = new List<byte[]>[SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.Length];

                // Initialize result.
                BothInvolvedTrigrams = new List<byte[]>[SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.Length][];
                FirstInvolvedTrigrams = new List<byte[]>[SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.Length][];

                for (int i = 0; i < BothInvolvedTrigrams.Count(); i++)
                {
                    BothInvolvedTrigrams[i] = new List<byte[]>[SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.Length];
                    FirstInvolvedTrigrams[i] = new List<byte[]>[SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.Length];
                    charToTrigram[i] = new List<byte[]>();

                    for (int j = 0; j < BothInvolvedTrigrams[i].Length; j++)
                    {
                        BothInvolvedTrigrams[i][j] = new();
                        FirstInvolvedTrigrams[i][j] = new();
                    }
                }

                var allTrigrams = SettingState.MeasurementSettings.SortedTrigramSet.SortedTrigramSet;

                // Create a mapping from characters to trigrams.
                for (int i = 0; i < allTrigrams.Count; i++)
                {
                    // Loop through every combination of characters
                    for (int j = 0; j < allTrigrams[i].Length; j++)
                    {
                        if (!charToTrigram[allTrigrams[i][j]].Contains(allTrigrams[i]))
                        {
                            charToTrigram[allTrigrams[i][j]].Add(allTrigrams[i]);
                        }
                    }
                }

                // Now create the resultant lists.
                for (byte i = 0; i < charToTrigram.Length; i++)
                {
                    for (byte j = 0; j < charToTrigram.Length; j++)
                    {
                        // Create the both list, we want to include in either order all the trigrams that contain both letters.
                        // All the trigrams that contain the first character (i) are in the first list and the ones that contain the second character (j) are in the other list.
                        for (int k = 0; k < charToTrigram[i].Count; k++)
                        {
                            // If the trigram contains the second character, it goes in the both category.
                            if (charToTrigram[i][k].Contains(j))
                            {
                                if (!BothInvolvedTrigrams[i][j].Contains(charToTrigram[i][k]))
                                {
                                    BothInvolvedTrigrams[i][j].Add(charToTrigram[i][k]);
                                }
                            }
                            else
                            {
                                if (!FirstInvolvedTrigrams[i][j].Contains(charToTrigram[i][k]))
                                {
                                    FirstInvolvedTrigrams[i][j].Add(charToTrigram[i][k]);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void SetToNewLimits()
        {
            TrySetToPending(false);
        }

        public override void SetToDefault()
        {
        }

        public override void SetToDesiredValue()
        {
        }

        protected override void Action()
        {
        }

        protected override void Load(string text)
        {
        }

        protected override void Save(XmlWriter writer)
        {
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            if (ValueHasChanged)
            {
                string description = $"Adjusted character to trigram set mappings";

                InitiateSettingChange(description, userInitiated, () =>
                {
                    mValueHasChanged = false;
                });
            }
        }
    }

    public class TrigramCoverageSetting : ConcreteValueSetting<uint>
    {
        public TrigramCoverageSetting() 
            : base("Trigram Coverage", 0, eSettingAttributes.None)
        {
        }

        protected override void Init()
        {
            SettingState.MeasurementSettings.SortedTrigramSet.AddDependent(this);
        }

        protected override void ModifyLimits()
        {
            var sortedTrigramSet = SettingState.MeasurementSettings.SortedTrigramSet.SortedTrigramSet;
            uint tgCoverage = 0;

            for(int i = 0; i < sortedTrigramSet.Count(); i++)
            {
                uint tgFreq = SettingState.MeasurementSettings.CharFrequencyData.
                    TrigramFreq[sortedTrigramSet[i][0]][sortedTrigramSet[i][1]][sortedTrigramSet[i][2]];

                tgCoverage += tgFreq;
            }

            mLimitValue = tgCoverage;
        }
    }
}
