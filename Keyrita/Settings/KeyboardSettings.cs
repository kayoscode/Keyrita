﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using Keyrita.Gui;
using Keyrita.Interop.NativeAnalysis;
using Keyrita.Serialization;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Settings
{
    public enum eKeyboardShape
    {
        [UIData("ANSI")]
        ANSI,
        [UIData("ISO")]
        ISO,
    }

    public enum eLang
    {
        [UIData("English")]
        English,
        [UIData("EnglishUK")]
        English_UK,
    }

    /// <summary>
    /// Modifies how the keybaord is shown in the UI.
    /// </summary>
    public enum eKeyboardDisplay
    {
        /// <summary>
        /// Displays the keyboard as a 3x10 grid.
        /// </summary>
        [UIData("Grid")]
        GridView,
        /// <summary>
        /// Displays the keyboard with the same offsets you would expect from a real keyboard.
        /// </summary>
        [UIData("Standard")]
        StandardView,
    }

    /// <summary>
    /// Stores information about the language based on the dataset.
    /// </summary>
    public class CharFrequencySetting : SettingBase
    {
        public CharFrequencySetting() 
            : base("Language Data", eSettingAttributes.None)
        {
        }

        protected string DatasetText { get; set; }

        protected uint[] CharFreq => mCharFreq;
        protected uint[] mCharFreq;
        protected long CharHitCount { get; set; }

        protected uint[,] BigramFreq => mBigramFreq;
        protected uint[,] mBigramFreq;
        protected long BigramHitCount { get; set; }

        protected uint[,,] TrigramFreq => mTrigramFreq;
        protected uint[,,] mTrigramFreq;
        protected long TrigramHitCount { get; set; }

        protected uint[,,] SkipgramFreq => mSkipgramFreq;
        protected uint[,,] mSkipgramFreq;
        protected long[] SkipgramHitCount { get; set; } = new long[NativeAnalysis.SKIPGRAM_DEPTH];

        public override bool HasValue => !(CharFreq == null || BigramFreq == null || TrigramFreq == null || SkipgramFreq == null);
        protected override bool ValueHasChanged => mValueHasChanged;
        private bool mValueHasChanged = false;

        protected override void SetDependencies()
        {
            // We only depend on the available char set.
            SettingState.KeyboardSettings.AvailableCharSet.AddDependent(this);
        }

        /// <summary>
        /// Load the dataset using the native code!
        /// </summary>
        /// <param name="fileName"></param>
        public void LoadDataset(string fileText)
        {
            if (fileText == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            CharHitCount = 0;
            BigramHitCount = 0;
            TrigramHitCount = 0;
            for(int i = 0; i < SkipgramHitCount.Length; i++)
            {
                SkipgramHitCount[i] = 0;
            }
            DatasetText = fileText;
            StringBuilder available = new StringBuilder();
            foreach (char c in SettingState.KeyboardSettings.AvailableCharSet.Collection)
            {
                available.Append(c);
            }

            long charCount = NativeAnalysis.AnalyzeDataset(fileText, available.ToString(), out mCharFreq, out mBigramFreq, out mTrigramFreq, out mSkipgramFreq);

#if(DEBUG)
            // Get the hit counts, hopefully this doesn't take too long to process.
            for(int i = 0; i < mCharFreq.Length; i++)
            {
                CharHitCount += mCharFreq[i];
            }

            for(int i = 0; i < mBigramFreq.GetLength(0); i++)
            {
                for(int j = 0; j < mBigramFreq.GetLength(1); j++)
                {
                    BigramHitCount += mBigramFreq[i, j];
                }
            }

            for(int i = 0; i < mTrigramFreq.GetLength(0); i++)
            {
                for(int j = 0; j < mTrigramFreq.GetLength(1); j++)
                {
                    for(int k = 0; k < mTrigramFreq.GetLength(2); k++)
                    {
                        TrigramHitCount += mTrigramFreq[i, j, k];
                    }
                }
            }

            for(int i = 0; i < mSkipgramFreq.GetLength(0); i++)
            {
                for(int j = 0; j < mSkipgramFreq.GetLength(1); j++)
                {
                    for(int k = 0; k < mSkipgramFreq.GetLength(2); k++)
                    {
                        SkipgramHitCount[i] += mSkipgramFreq[i, j, k];
                    }
                }
            }

            var exCharHitCount = charCount;
            LTrace.Assert(exCharHitCount == CharHitCount, "Sanity check failed");
            var exBigramHitCount = CharHitCount - 1;
            LTrace.Assert(exBigramHitCount == BigramHitCount, "Sanity check failed");
            var exTrigramHitCount = BigramHitCount - 1;
            LTrace.Assert(exTrigramHitCount == TrigramHitCount, "Sanity check failed");

            var exSkipgramHitCount = new long[SkipgramHitCount.Length];
            exSkipgramHitCount[0] = TrigramHitCount - 1;
            LTrace.Assert(exSkipgramHitCount[0] == SkipgramHitCount[0], "Sanity check failed");

            for(int i = 1; i < SkipgramHitCount.Length; i++)
            {
                exSkipgramHitCount[i] = SkipgramHitCount[i - 1] - 1;
                LTrace.Assert(exSkipgramHitCount[i] == SkipgramHitCount[i], "Sanity check failed");
            }
#else
            CharHitCount = charCount;
            BigramHitCount = CharHitCount - 1;
            TrigramHitCount = BigramHitCount - 1;
            SkipgramHitCount[0] = TrigramHitCount - 1;

            for(int i = 1; i < SkipgramHitCount.Length; i++)
            {
                SkipgramHitCount[i] = SkipgramHitCount[i - 1] - 1;
            }
#endif

            mValueHasChanged = true;
            sw.Stop();
            long time = sw.ElapsedMilliseconds;
            LTrace.LogInfo($"Analyzed dataset in {time} milliseconds");
            TrySetToPending();
        }

        public override void SetToDefault()
        {
            // TODO: Load default dataset.
        }

        public override void SetToDesiredValue()
        {
            // The desired value is whatever the heck it is currently set to.
        }

        protected override void Action()
        {
            // Nothing to do.
        }

        protected override void ChangeLimits()
        {
            // This setting doesn't use a pending value!
        }

        protected override void Load(string text)
        {
            // Too much data to save/load.
        }

        protected override void Save(XmlWriter writer)
        {
            // Too much data to save/load.
        }

        protected override void SetToNewLimits()
        {
            // Reanalyze the dataset :( -> We've gotta do it
            LoadDataset(DatasetText);
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            // We aren't using a pending value, so lets just report a settings transaction occurred.
            if (ValueHasChanged)
            {
                SettingTransaction("Analyzing dataset", false, () =>
                {
                    // Now that we have reported the setting change, just make it so that we report the setting hasn't changed yet.
                    mValueHasChanged = false;
                });
            }
        }
    }

    /// <summary>
    /// Analyzes the keyboard when the keyboard's state changes, and the 
    /// analyzer is running.
    /// </summary>
    public class KeyboardAnalysisAction : ActionSetting
    {
        public KeyboardAnalysisAction()
            :base("Trigger Analysis")
        {
        }

        protected override void SetDependencies()
        {
            SettingState.MeasurementSettings.AnalysisEnabled.AddDependent(this);
            SettingState.KeyboardSettings.KeyboardState.AddDependent(this);
            SettingState.MeasurementSettings.CharFrequencyData.AddDependent(this);
        }

        protected override void DoAction()
        {
            if(SettingState.MeasurementSettings.AnalysisEnabled.Value.Equals(eOnOff.On))
            {
                // Todo: Perform keyboard analysis.
            }
        }
    }

    /// <summary>
    /// Whether the keyboard should be analyzed after a single change or not.
    /// If off, analysis will not take place until this is turned on.
    /// </summary>
    public class AnalysisEnabledSetting : OnOffSetting
    {
        public AnalysisEnabledSetting() 
            : base("Analysis Enabled", eOnOff.On, eSettingAttributes.Recall)
        {
        }

        protected override void SetDependencies()
        {
            SettingState.KeyboardSettings.KeyboardValid.AddDependent(this);
        }

        protected override void ChangeLimits()
        {
            mValidTokens.Clear();

            if (SettingState.KeyboardSettings.KeyboardValid.Value.Equals(eOnOff.On))
            {
                mValidTokens.Add(eOnOff.On);
                mValidTokens.Add(eOnOff.Off);
            }
            else
            {
                mValidTokens.Add(eOnOff.Off);
            }
        }
    }

    /// <summary>
    /// Set to On if the keyboard state is valid. Meaning it only contains characters in the current language.
    /// </summary>
    public class KeyboardValidSetting : OnOffSetting
    {
        public KeyboardValidSetting()
            : base("Keyboard Valid", eOnOff.Off, eSettingAttributes.None)
        {
        }

        protected override void SetDependencies()
        {
            // We depend on the keyboard state, and the language.
            SettingState.KeyboardSettings.AvailableCharSet.AddDependent(this);
            SettingState.KeyboardSettings.KeyboardState.AddDependent(this);
        }

        /// <summary>
        /// Scan the keyboard and see if we are valid.
        /// </summary>
        protected override void ChangeLimits()
        {
            mValidTokens.Clear();

            // Scan the keyboard's active state and make sure that each character is in the current language set.
            // And check that it has no repeat characters.
            var characterSet = SettingState.KeyboardSettings.AvailableCharSet.Collection;

            Dictionary<char, bool> charHasBeenUsed = new Dictionary<char, bool>();
            foreach(char c in characterSet)
            {
                charHasBeenUsed[c] = false;
            }

            bool valid = true;
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    char character = SettingState.KeyboardSettings.KeyboardState.GetCharacterAt(i, j);

                    if(charHasBeenUsed.TryGetValue(character, out bool used) && !used)
                    {
                        charHasBeenUsed[character] = true;
                    }
                    else
                    {
                        valid = false;
                    }
                }
            }

            if(valid)
            {
                mValidTokens.Add(eOnOff.On);
            }
            else
            {
                mValidTokens.Add(eOnOff.Off);
            }
        }
    }

    /// <summary>
    /// The shape of the keyboard setting.
    /// </summary>
    public class KeyboardShapeSetting : EnumValueSetting<eKeyboardShape>
    {
        public KeyboardShapeSetting()
            : base("Keyboard Shape", eKeyboardShape.ANSI, eSettingAttributes.Recall)
        {
        }

        protected override void SetDependencies()
        {
            SettingState.KeyboardSettings.KeyboardLanguage.AddDependent(this);
        }

        protected override void ChangeLimits()
        {
            mValidTokens.Clear();

            mValidTokens.Add(eKeyboardShape.ANSI);
            mValidTokens.Add(eKeyboardShape.ISO);
        }

        public override Enum DefaultValue
        {
            get
            {
                if (SettingState.KeyboardSettings.KeyboardLanguage.DefaultValue.Equals(eLang.English))
                {
                    return eKeyboardShape.ANSI;
                }

                return eKeyboardShape.ISO;
            }
        }
    }

    /// <summary>
    /// The way the keyboard is displayed to the user.
    /// </summary>
    public class KeyboardDisplaySetting : EnumValueSetting<eKeyboardDisplay>
    {
        public KeyboardDisplaySetting()
            : base("Keyboard View", eKeyboardDisplay.StandardView, eSettingAttributes.Recall)
        {
        }
    }

    /// <summary>
    /// Whether or not to show measurement annotations for the selected measurement if available.
    /// </summary>
    public class KeyboardShowAnnotationsSetting : OnOffSetting
    {
        public KeyboardShowAnnotationsSetting() : 
            base("Show Annotations", eOnOff.Off, eSettingAttributes.Recall)
        {
        }
    }

    /// <summary>
    /// The current language of the keyboard.
    /// </summary>
    public class KeyboardLanguageSetting : EnumValueSetting<eLang>
    {
        public KeyboardLanguageSetting() : 
            base("Language", eLang.English, eSettingAttributes.Recall)
        {
        }
    }

    /// <summary>
    /// Setting to store the fraction of a key offset a row has from the standard position.
    /// </summary>
    public class RowHorizontalOffsetSetting : ConcreteValueSetting<double>
    {
        protected int RowIndex;
        protected const double STANDARD_OFFSET_TOP = -.25;
        protected const double STANDARD_OFFSET_MIDDLE = 0;
        protected const double STANDARD_OFFSET_BOTTOM = .5;
        protected readonly double[] STANDARD_ROW_OFFSETS =
        {
            STANDARD_OFFSET_TOP,
            STANDARD_OFFSET_MIDDLE,
            STANDARD_OFFSET_BOTTOM
        };

        public RowHorizontalOffsetSetting(int rowIndex) 
            : base("Row " + rowIndex + " Offset", 0.0, eSettingAttributes.None)
        {
            LTrace.Assert(rowIndex < 3, "Only three rows supported");

            this.RowIndex = rowIndex;
            mLimitValue = STANDARD_ROW_OFFSETS[rowIndex];
        }

        protected override void SetDependencies()
        {
            SettingState.KeyboardSettings.KeyboardDisplay.AddDependent(this);
        }

        protected override void ChangeLimits()
        {
            if (SettingState.KeyboardSettings.KeyboardDisplay.Value.Equals(eKeyboardDisplay.GridView))
            {
                mLimitValue = 0.0;
            }
            else
            {
                mLimitValue = STANDARD_ROW_OFFSETS[RowIndex];
            }
        }
    }

    /// <summary>
    /// Shows the characters currently available to the user.
    /// </summary>
    public class CharacterSetSetting : ElementSetSetting<char>
    {
        private string EnglishCharSet = "qwertyuiopasdfghjkl;zxcvbnm,./'";
        private string EnglishUKCharSet = "wertyuiopasdfghjkl;zxcvbnm,./'";

        public CharacterSetSetting() 
            : base("Available Character Set", eSettingAttributes.None)
        {
            // Default should just be english.
            foreach (char ch in EnglishCharSet)
            {
                mDefaultCollection.Add(ch);
            }
        }

        protected override void SetDependencies()
        {
            SettingState.KeyboardSettings.KeyboardLanguage.AddDependent(this);
        }

        protected override void ChangeLimits(ISet<char> newLimits)
        {
            switch (SettingState.KeyboardSettings.KeyboardLanguage.Value)
            {
                case eLang.English:
                    foreach (char ch in EnglishCharSet)
                    {
                        newLimits.Add(ch);
                    }
                    break;
                case eLang.English_UK:
                    foreach (char ch in EnglishUKCharSet)
                    {
                        newLimits.Add(ch);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// The list of keys
    /// </summary>
    public class KeyboardStateSetting : SettingBase
    {
        public const int ROWS = 3;
        public const int COLS = 10;

        public KeyboardStateSetting() : 
            base("Keyboard State", eSettingAttributes.Recall)
        {
        }

        protected char[,] mKeyboardState = new char[ROWS, COLS];
        protected char[,] mNewKeyboardState = new char[ROWS, COLS];
        protected char[,] mPendingKeyboardState = new char[ROWS, COLS];
        protected char[,] mDesiredKeyboardState = new char[ROWS, COLS];

        public override bool HasValue => mKeyboardState != null;
        protected override bool ValueHasChanged => BoardsMatch(mPendingKeyboardState, mKeyboardState) > 0;

        #region Public manipulation interface

        /// <summary>
        /// Reflects the keyboard state along the x axis.
        /// </summary>
        public void ReflectHorz()
        {
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS / 2; j++)
                {
                    var reflectJ = COLS - j - 1;

                    mPendingKeyboardState[i, reflectJ] = mKeyboardState[i, j];
                    mPendingKeyboardState[i, j] = mKeyboardState[i, reflectJ];
                }
            }

            TrySetToPending(true);
        }

        public void ReflectVert()
        {
            for (int i = 0; i < ROWS / 2; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    var reflectI = ROWS - i - 1;

                    mPendingKeyboardState[reflectI, j] = mKeyboardState[i, j];
                    mPendingKeyboardState[i, j] = mKeyboardState[reflectI, j];
                }
            }

            TrySetToPending(true);
        }

        #endregion

        protected override void Load(string text)
        {
            string[] layout = text.Split(" ");

            int index = 0;

            foreach(string s in layout)
            {
                int row = index / 10;
                int col = index % 10;

                if(TextSerializers.TryParse(s, out char nextCharacter))
                {
                    mDesiredKeyboardState[row, col] = nextCharacter;
                }

                index++;
            }

            TrySetToPending();
        }

        protected override void Save(XmlWriter writer)
        {
            // Convert the enum value to a string and write it to the stream writer.
            string uniqueName = this.GetSettingUniqueId();
            writer.WriteStartElement(uniqueName);

            for(int i = 0; i < ROWS; i++)
            {
                for(int j = 0; j < COLS; j++)
                {
                    writer.WriteString(TextSerializers.ToText(mKeyboardState[i, j]));
                    writer.WriteString(" ");
                }
            }

            writer.WriteEndElement();
        }

        protected override void Action()
        {
        }
        
        /// <summary>
        /// Returns the character at a specified index.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public char GetCharacterAt(int row, int col)
        {
            return mKeyboardState[row, col];
        }

        protected override void SetDependencies()
        {
            // We care about the keyboard format and language.
            SettingState.KeyboardSettings.AvailableCharSet.AddDependent(this);
        }

        protected override void ChangeLimits()
        {
            ChangeLimits(mNewKeyboardState);
        }

        protected virtual void ChangeLimits(char[,] newLimits)
        {
            // Todo: When the langauge changes, translate each character to its other langauge equivalent.
            // For now, just set it to default.
            var availableCharacters = SettingState.KeyboardSettings.AvailableCharSet.Collection;
            var chars = string.Join("", availableCharacters);
            chars = chars.PadRight(30, '*');

            // There should be at least 30 characters in the set of available chars.
            LTrace.Assert(chars.Length >= 30);

            int strIndex = 0;
            for(int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    newLimits[i, j] = chars[strIndex++];
                }
            }
        }

        /// <summary>
        /// Sets the keyboard layout to desired.
        /// </summary>
        public override void SetToDesiredValue()
        {
            CopyBoard(mPendingKeyboardState, mDesiredKeyboardState);
            TrySetToPending();
        }

        /// <summary>
        /// Sets to the default: qwerty.
        /// </summary>
        public override void SetToDefault()
        {
            var availableCharacters = SettingState.KeyboardSettings.AvailableCharSet.DefaultCollection;
            var chars = string.Join("", availableCharacters);
            chars = chars.PadRight(30, '*');

            // There should be at least 30 characters in the set of available chars.
            LTrace.Assert(chars.Length >= 30);

            int strIndex = 0;
            for(int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    mDesiredKeyboardState[i, j] = chars[strIndex++];
                }
            }
        }

        protected override void SetToNewLimits()
        {
            // Copy the pending board to the new layout.
            CopyBoard(mPendingKeyboardState, mNewKeyboardState);
            TrySetToPending();
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            // If the pending keyboard state does not match the current keyboard state, start a setting transaction.
            var count = BoardsMatch(mPendingKeyboardState, mKeyboardState);

            if (count != 0)
            {
                var description = $"Changing {count} keys in the keyboard layout";

                SettingTransaction(description, userInitiated, () =>
                {
                    CopyBoard(mKeyboardState, mPendingKeyboardState);
                });
            }
        }

        /// <summary>
        /// Copies keyboard2 to keyboard1.
        /// </summary>
        /// <param name="kb1"></param>
        /// <param name="kb2"></param>
        private static void CopyBoard(char[,] kb1, char[,] kb2)
        {
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    kb1[i, j] = kb2[i, j];
                }
            }
        }

        /// <summary>
        /// Returns whether two boards have the same state.
        /// Specifically returns the number of keys that don't match.
        /// </summary>
        /// <param name="kb1"></param>
        /// <param name="kb2"></param>
        /// <returns></returns>
        private static int BoardsMatch(char[,] kb1, char[,] kb2)
        {
            var count = 0;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (kb1[i, j] != kb2[i, j])
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
