using Keyrita.Gui;
using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
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
    public interface IEnumValueSetting : ISetting
    {
        Enum Value { get; }
        Enum DefaultValue { get; }
    }

    public abstract class EnumValueSetting : SettingBase
    {
        public INotifyCollectionChanged mValidTokensChanged;

        protected EnumValueSetting(string settingName, Enum defaultValue, eSettingAttributes attributes) 
            : base(settingName, attributes)
        {
            DefaultValue = defaultValue;
            DesiredValue = DefaultValue;
        }

        public Enum Value { get; private set; }

        public virtual Enum DefaultValue { get; private set; }

        protected Enum DesiredValue { get; set; }
        protected Enum PendingValue { get; set; }

        protected IList<Enum> mValidTokens { get; } = new List<Enum>();
        public IReadOnlyList<Enum> ValidTokens
        {
            get
            {
                IReadOnlyList<Enum> validTokens = (IReadOnlyList<Enum>)mValidTokens;
                return validTokens;
            }
        }

        /// <summary>
        /// Whether there is a specific value in this setting.
        /// </summary>
        public override bool HasValue { get; protected set; }

        protected override bool ValueHasChanged
        {
            get
            {
                if(Value != null && PendingValue != null)
                {
                    return !PendingValue.Equals(Value);
                }
                else if(Value == null && PendingValue != null)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts to set the value of the setting.
        /// </summary>
        /// <param name="value"></param>
        public void Set(Enum value)
        {
            this.DesiredValue = value;
            this.PendingValue = value;

            TrySetToPending();
        }

        /// <summary>
        /// Returns the enum value which is at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Enum GetTokenAtIndex(int index)
        {
            if(index < mValidTokens.Count)
            {
                return mValidTokens[index];
            }

            LTrace.Assert(false);
            return null;
        }

        /// <summary>
        /// Returns the index of the currently selected item.
        /// </summary>
        /// <returns></returns>
        public int GetIndexOfSelection()
        {
            if(HasValue)
            {
                return mValidTokens.IndexOf(Value);
            }

            return -1;
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
            TrySetToPending();
        }

        protected override void SetToNewLimits()
        {
            if(mValidTokens.Contains(DesiredValue))
            {
                PendingValue = DesiredValue;
                TrySetToPending();
                return;
            }

            if (!mValidTokens.Contains(Value))
            {
                if(mValidTokens.Contains(DefaultValue))
                {
                    SetToDefault();
                }
                else
                {
                    if(mValidTokens.Count > 0)
                    {
                        PendingValue = mValidTokens.First<Enum>();
                        TrySetToPending();
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
            if (!PendingValue.Equals(Value) && mValidTokens.Contains(PendingValue))
            {
                SettingTransaction($"Setting token value from {Value} to {PendingValue}", () =>
                {
                    Value = PendingValue;
                    HasValue = true;
                });
            }
        }
    }

    /// <summary>
    /// A setting which can hold one of the values of an enumeration.
    /// </summary>
    public abstract class EnumValueSetting<TEnum> : EnumValueSetting, IEnumValueSetting
        where TEnum: Enum
    {
        protected EnumValueSetting(string settingName, TEnum defaultValue, eSettingAttributes attributes)
            : base(settingName, defaultValue, attributes)
        {
            mValidTokens.Clear();

            foreach (TEnum token in EnumUtils.GetTokens(typeof(TEnum)))
            {
                mValidTokens.Add(token);
            }
        }
    }

    /// <summary>
    /// Readonly interface to an on off setting.
    /// </summary>
    public interface IOnOffSetting : IEnumValueSetting
    {
        bool IsOn { get; }
        bool IsOff { get; }
    }

    /// <summary>
    /// Setting holding an on or off state.
    /// </summary>
    public abstract class OnOffSetting : EnumValueSetting<eOnOff>, IEnumValueSetting, IOnOffSetting
    {
        public OnOffSetting(string settingName, eSettingAttributes attributes) 
            : this(settingName, eOnOff.Off, attributes)
        {
        }

        public OnOffSetting(string settingName, eOnOff defaultValue, eSettingAttributes attributes)
            : base(settingName, defaultValue, attributes)
        {
        }

        public bool IsOn => HasValue && Value.Equals(eOnOff.On);
        public bool IsOff => HasValue && Value.Equals(eOnOff.Off);
    }
}
