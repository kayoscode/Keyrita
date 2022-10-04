using Keyrita.Gui;
using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace Keyrita.Settings
{
    /// <summary>
    /// On and off enumeration states.
    /// </summary>
    public enum eOnOff
    {
        [UIData("Off")]
        Off,
        [UIData("On")]
        On
    }

    /// <summary>
    /// Readonly interface to an enum value setting.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEnumValueSetting<TEnum> : ISetting
    {
        TEnum Value { get; }
        TEnum DefaultValue { get; }
    }

    /// <summary>
    /// A setting which can hold one of the values of an enumeration.
    /// </summary>
    public abstract class EnumValueSetting<TEnum> : SettingBase, IEnumValueSetting<TEnum>
    {
        protected EnumValueSetting(string settingName, TEnum defaultValue, eSettingAttributes attributes) 
            : base(settingName, attributes)
        {
            DefaultValue = defaultValue;
            ValidTokens = EnumUtils.GetTokens(typeof(TEnum)).Cast<TEnum>().ToList();
        }

        public TEnum Value { get; private set; }

        public virtual TEnum DefaultValue { get; private set; }

        private TEnum DesiredValue { get; set; }
        private TEnum PendingValue { get; set; }

        private IList<TEnum> ValidTokens { get; } = new List<TEnum>();

        /// <summary>
        /// Whether there is a specific value in this setting.
        /// </summary>
        public override bool HasValue { get; protected set; }

        protected override bool ValueHasChanged
        {
            get
            {
                return !PendingValue.Equals(Value);
            }
        }

        public void Set(TEnum value)
        {
            this.DesiredValue = value;
            this.PendingValue = value;

            TrySetToPending();
        }

        public override void Load()
        {
            throw new NotImplementedException();
        }

        public override void Save()
        {
            throw new NotImplementedException();
        }

        protected override void Action()
        {
        }

        protected override void ChangeLimits()
        {
        }

        protected override void SetToDefault()
        {
            PendingValue = DefaultValue;
        }

        protected override void SetToNewLimits()
        {
            if (!ValidTokens.Contains(Value))
            {
                if(ValidTokens.Contains(DefaultValue))
                {
                    SetToDefault();
                }
                else
                {
                    if(ValidTokens.Count > 0)
                    {
                        PendingValue = ValidTokens.First<TEnum>();
                    }
                    else
                    {
                        HasValue = false;
                    }
                }
            }
        }

        protected override void TrySetToPending()
        {
            // If the values don't match, we need to initialize a new setting transaction.
            if (!PendingValue.Equals(Value))
            {
                SettingTransaction($"Setting token value from {Value} to {PendingValue}", () =>
                {
                    Value = PendingValue;
                });
            }
        }
    }

    /// <summary>
    /// Readonly interface to an on off setting.
    /// </summary>
    public interface IOnOffSetting : IEnumValueSetting<eOnOff>
    {
        bool IsOn { get; }
        bool IsOff { get; }
    }

    /// <summary>
    /// Setting holding an on or off state.
    /// </summary>
    public abstract class OnOffSetting : EnumValueSetting<eOnOff>, IEnumValueSetting<eOnOff>
    {
        public OnOffSetting(string settingName, eSettingAttributes attributes) 
            : this(settingName, eOnOff.Off, attributes)
        {
        }

        public OnOffSetting(string settingName, eOnOff defaultValue, eSettingAttributes attributes)
            : base(settingName, defaultValue, attributes)
        {
        }

        public bool IsOn => HasValue && Value == eOnOff.On;
        public bool IsOff => HasValue && Value == eOnOff.Off;
    }
}
