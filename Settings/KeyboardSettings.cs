using Keyrita.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    /// Readonly interface to keyboard settings.
    /// </summary>
    public interface IKeyboardSettings
    {
        IEnumValueSetting KeyboardShape { get; }
    }

    public interface IMeasurementSettings
    {
        IOnOffSetting ShowAnnotations { get; }
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
    /// The list of keys
    /// </summary>
    public class KeyboardStateSetting : SettingBase
    {
        public KeyboardStateSetting() : 
            base("Keyboard State", eSettingAttributes.None)
        {
        }

        protected char[,] KeyboardState = new char[3, 10];

        public override bool HasValue => KeyboardState != null;

        protected override bool ValueHasChanged { get; } = false;

        public override void Load()
        {
        }

        public override void Save()
        {
        }

        protected override void Action()
        {
        }

        protected override void SetDependencies()
        {
            // We care about the keyboard format and language.
            SettingState.KeyboardSettings.KeyboardLanguage.AddDependent(this);
        }

        protected override void ChangeLimits()
        {
            // Todo: When the langauge changes, translate each character to its other langauge equivalent.
        }

        protected override void SetToDefault()
        {
            throw new NotImplementedException();
        }

        protected override void SetToNewLimits()
        {
            throw new NotImplementedException();
        }

        protected override void TrySetToPending()
        {
            throw new NotImplementedException();
        }
    }
}
