using System;
using System.Collections.Generic;
using System.Xml;
using Keyrita.Gui;
using Keyrita.Operations.OperationUtil;
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
        [UIData("Grid", "Keys are lined up vertically")]
        GridView,
        /// <summary>
        /// Displays the keyboard with the same offsets you would expect from a real keyboard.
        /// </summary>
        [UIData("Standard", "Displays the keys offset as they would be on a standard keyboard")]
        StandardView,
    }

    public enum eKeyboardEditMode
    {
        /// <summary>
        /// Editing by moving keys around.
        /// </summary>
        Normal,
        /// <summary>
        /// Editing by changing the effort value for pressing each key.
        /// </summary>
        EffortMap,
        /// <summary>
        /// Editing by changing the scissor map.
        /// </summary>
        ScissorMap
    }

    /// <summary>
    /// All options are valid at all times.
    /// </summary>
    public class KeyboardEditMode : EnumValueSetting<eKeyboardEditMode>
    {
        public KeyboardEditMode() 
            : base("Edit Mode", eKeyboardEditMode.Normal, eSettingAttributes.Recall)
        {
        }
    }

    /// <summary>
    /// Whether or not to show which fingers are being used.
    /// </summary>
    public class ShowFingerUsage : OnOffSetting
    {
        public ShowFingerUsage() : 
            base("Show Finger Usage", eOnOff.On, eSettingAttributes.Recall)
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Colors each key to indicate which finger will be used to click the key";
            }
        }
    }

    /// <summary>
    /// Which type of heatmap should be displayed to the user.
    /// </summary>
    public class HeatmapSetting : EnumValueSetting<eHeatMap>
    {
        public HeatmapSetting() 
            : base("Heatmap Type", eHeatMap.CharacterFrequency, eSettingAttributes.Recall)
        {
        }
    }

    /// <summary>
    /// Analyzes the keyboard when the keyboard's state changes, and the 
    /// analyzer is running.
    /// </summary>
    public class KeyboardAnalysisAction : ActionSetting
    {
        public KeyboardAnalysisAction()
            : base("Trigger Analysis")
        {
        }

        protected override void SetDependencies()
        {
            SettingState.KeyboardSettings.KeyboardState.AddDependent(this);
            SettingState.KeyboardSettings.KeyboardValid.AddDependent(this);
            SettingState.MeasurementSettings.AnalysisEnabled.AddDependent(this);
        }

        protected override void DoAction()
        {
            if(SettingState.MeasurementSettings.AnalysisEnabled.HasValue && SettingState.KeyboardSettings.KeyboardValid.HasValue)
            {
                if (SettingState.MeasurementSettings.AnalysisEnabled.Value.Equals(eOnOff.On) &&
                   SettingState.KeyboardSettings.KeyboardValid.Value.Equals(eOnOff.On))
                {
                    // Resolve the ops.
                    AnalysisGraphSystem.ResolveGraph();
                }
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
            foreach (char c in characterSet)
            {
                charHasBeenUsed[c] = false;
            }

            bool valid = true;
            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    char character = SettingState.KeyboardSettings.KeyboardState.GetValueAt(i, j);

                    if (charHasBeenUsed.TryGetValue(character, out bool used) && !used)
                    {
                        charHasBeenUsed[character] = true;
                    }
                    else
                    {
                        valid = false;
                    }
                }
            }

            if (valid)
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
            LogUtils.Assert(rowIndex < KeyboardStateSetting.ROWS, "Only three rows supported");

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
        private string EnglishCharSet = "qwertyuiopasdfghjkl;zxcvbnm,./' \\-=";
        private string EnglishUKCharSet = "qwertyuiopasdfghjkl;zxcvbnm,./' \\-=";

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

    public abstract class PerkeySetting<T> : SettingBase
    {
        public const int ROWS = 3;
        public const int COLS = 10;

        public PerkeySetting(string settingName, eSettingAttributes attributes) :
            base(settingName, attributes)
        {
        }

        protected T[,] mKeyState = new T[ROWS, COLS];
        protected T[,] mNewKeyState = new T[ROWS, COLS];
        protected T[,] mPendingKeyState = new T[ROWS, COLS];
        protected T[,] mDesiredKeyState = new T[ROWS, COLS];

        public override bool HasValue => mKeyState != null;
        protected override bool ValueHasChanged => BoardsMatch(mPendingKeyState, mKeyState) > 0;

        /// <summary>
        /// Copies the key state to an array
        /// </summary>
        public T[,] KeyStateCopy
        {
            get
            {
                T[,] ret = new T[ROWS, COLS];

                for (int i = 0; i < ROWS; i++)
                {
                    for (int j = 0; j < COLS; j++)
                    {
                        ret[i, j] = mKeyState[i, j];
                    }
                }

                return ret;
            }
        }

        protected override void Load(string text)
        {
            string[] layout = text.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            int index = 0;

            foreach (string s in layout)
            {
                int row = index / 10;
                int col = index % 10;

                if (TextSerializers.TryParse(s, out T nextCharacter))
                {
                    mDesiredKeyState[row, col] = nextCharacter;
                }

                index++;
            }
        }

        protected override void Save(XmlWriter writer)
        {
            // Convert the enum value to a string and write it to the stream writer.
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    writer.WriteString(TextSerializers.ToText(mKeyState[i, j]));
                    writer.WriteString(" ");
                }
            }
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
        public T GetValueAt(int row, int col)
        {
            return mKeyState[row, col];
        }

        protected override sealed void ChangeLimits()
        {
            ChangeLimits(mNewKeyState);
        }

        protected virtual void ChangeLimits(T[,] values)
        {
        }

        /// <summary>
        /// Sets the keyboard layout to desired.
        /// </summary>
        public override sealed void SetToDesiredValue()
        {
            CopyBoard(mPendingKeyState, mDesiredKeyState);
            TrySetToPending();
        }


        protected override sealed void SetToNewLimits()
        {
            // Copy the pending board to the new layout.
            CopyBoard(mPendingKeyState, mNewKeyState);
            TrySetToPending();
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            // If the pending keyboard state does not match the current keyboard state, start a setting transaction.
            var count = BoardsMatch(mPendingKeyState, mKeyState);

            if (count != 0)
            {
                var description = $"Changing {count} keys";

                SettingTransaction(description, userInitiated, () =>
                {
                    CopyBoard(mKeyState, mPendingKeyState);
                });
            }
        }

        /// <summary>
        /// Copies keyboard2 to keyboard1.
        /// </summary>
        /// <param name="kb1"></param>
        /// <param name="kb2"></param>
        protected static void CopyBoard(T[,] kb1, T[,] kb2)
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
        public static int BoardsMatch(T[,] kb1, T[,] kb2)
        {
            var count = 0;

            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(kb1[i,j], kb2[i, j]))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

    /// <summary>
    /// The list of keys
    /// </summary>
    public class KeyboardStateSetting : PerkeySetting<char>
    {
        public KeyboardStateSetting() 
            :base("Keyboard State", eSettingAttributes.Recall)
        {

        }

        protected override void ChangeLimits(char[,] newLimits)
        {
            // Todo: When the langauge changes, translate each character to its other langauge equivalent.
            // For now, just set it to default.
            var availableCharacters = SettingState.KeyboardSettings.AvailableCharSet.Collection;
            var chars = string.Join("", availableCharacters);
            chars = chars.PadRight(30, '*');

            // There should be at least 30 characters in the set of available chars.
            LogUtils.Assert(chars.Length >= 30);

            int strIndex = 0;
            for (int i = 0; i < ROWS; i++)
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
        public override void SetToDefault()
        {
            var availableCharacters = SettingState.KeyboardSettings.AvailableCharSet.DefaultCollection;
            var chars = string.Join("", availableCharacters);
            chars = chars.PadRight(30, '*');

            // There should be at least 30 characters in the set of available chars.
            LogUtils.Assert(chars.Length >= 30);

            int strIndex = 0;
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    mDesiredKeyState[i, j] = chars[strIndex++];
                }
            }
            SetToDesiredValue();
        }

        protected override void SetDependencies()
        {
            // We care about the keyboard format and language.
            SettingState.KeyboardSettings.AvailableCharSet.AddDependent(this);
        }

        #region Public manipulation interface

        public void SwapKeys(char k1, char k2)
        {
            for (int i = 0; i < ROWS; i++)
            {
                for(int j = 0; j < COLS; j++)
                {
                    if (mKeyState[i, j] == k1)
                    {
                        mPendingKeyState[i, j] = k2;
                    }
                    else if (mKeyState[i, j] == k2)
                    {
                        mPendingKeyState[i, j] = k1;
                    }
                }
            }

            CopyBoard(mDesiredKeyState, mPendingKeyState);
            TrySetToPending(true);
        }

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

                    mPendingKeyState[i, reflectJ] = mKeyState[i, j];
                    mPendingKeyState[i, j] = mKeyState[i, reflectJ];
                }
            }

            CopyBoard(mDesiredKeyState, mPendingKeyState);
            TrySetToPending(true);
        }

        public void ReflectVert()
        {
            for (int i = 0; i < ROWS / 2; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    var reflectI = ROWS - i - 1;

                    mPendingKeyState[reflectI, j] = mKeyState[i, j];
                    mPendingKeyState[i, j] = mKeyState[reflectI, j];
                }
            }

            CopyBoard(mDesiredKeyState, mPendingKeyState);
            TrySetToPending(true);
        }

        #endregion
    }
}
