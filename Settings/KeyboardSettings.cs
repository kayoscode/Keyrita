using Keyrita.Gui;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using System;
using System.Collections.Generic;

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
            : base("Analysis Enabled", eOnOff.On, eSettingAttributes.None)
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
            : base("Keyboard Shape", eKeyboardShape.ANSI, eSettingAttributes.None)
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
            : base("Keyboard View", eKeyboardDisplay.StandardView, eSettingAttributes.None)
        {
        }
    }

    /// <summary>
    /// Whether or not to show measurement annotations for the selected measurement if available.
    /// </summary>
    public class KeyboardShowAnnotationsSetting : OnOffSetting
    {
        public KeyboardShowAnnotationsSetting() : 
            base("Show Annotations", eOnOff.Off, eSettingAttributes.None)
        {
        }
    }

    /// <summary>
    /// The current language of the keyboard.
    /// </summary>
    public class KeyboardLanguageSetting : EnumValueSetting<eLang>
    {
        public KeyboardLanguageSetting() : 
            base("Language", eLang.English, eSettingAttributes.None)
        {
        }
    }

    /// <summary>
    /// Setting to store the fraction of a key offset a row has from the standard position.
    /// </summary>
    public class RowHorizontalOffsetSetting : ConcreteValueSetting<double>
    {
        protected int rowIndex;
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

            this.rowIndex = rowIndex;
            mDefaultValue = STANDARD_ROW_OFFSETS[rowIndex];
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
                mLimitValue = STANDARD_ROW_OFFSETS[rowIndex];
            }
        }
    }

    /// <summary>
    /// Shows the characters currently available to the user.
    /// </summary>
    public class CharacterSetSetting : ElementSetSetting<char>
    {
        private string EnglishCharSet = "qwertyuiopasdfghjkl;zxcvbnm,./'";
        private string EnglishUKCharSet = "qwertyuiopasdfghjkl;zxcvbnm,./'";

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

        protected override void ChangeLimits(ISet<object> newLimits)
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
            base("Keyboard State", eSettingAttributes.None)
        {
        }

        protected char[,] mKeyboardState = new char[ROWS, COLS];
        protected char[,] mNewKeyboardState = new char[ROWS, COLS];
        protected char[,] mPendingKeyboardState = new char[ROWS, COLS];

        public override bool HasValue => mKeyboardState != null;
        protected override bool ValueHasChanged => BoardsMatch(mPendingKeyboardState, mKeyboardState) > 0;

        public override void Load()
        {
        }

        public override void Save()
        {
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
        /// Sets to the default: qwerty.
        /// </summary>
        protected override void SetToDefault()
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
                    mPendingKeyboardState[i, j] = chars[strIndex++];
                }
            }
        }

        protected override void SetToNewLimits()
        {
            // Copy the pending board to the new layout.
            CopyBoard(mPendingKeyboardState, mNewKeyboardState);
            TrySetToPending();
        }

        protected override void TrySetToPending()
        {
            // If the pending keyboard state does not match the current keyboard state, start a setting transaction.
            var count = BoardsMatch(mPendingKeyboardState, mKeyboardState);

            if (count != 0)
            {
                var description = $"Changing {count} keys in the keyboard layout";

                SettingTransaction(description, () =>
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
