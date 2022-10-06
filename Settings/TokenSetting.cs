using Keyrita.Gui;
using Keyrita.Serialization;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml;

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
        bool HasValue { get; }
    }

    public abstract class EnumValueSetting : SettingBase
    {
        protected EnumValueSetting(string settingName, Enum defaultValue, eSettingAttributes attributes) 
            : this(settingName, attributes)
        {
            mDefaultValue = defaultValue;
        }

        protected EnumValueSetting(string settingName, eSettingAttributes attributes)
            : base(settingName, attributes)
        {
        }

        public override void PreInitialization()
        {
            base.PreInitialization();
        }

        public Enum Value { get; private set; }

        public virtual Enum DefaultValue => mDefaultValue;
        private Enum mDefaultValue;

        protected Enum DesiredValue { get; set; }
        protected Enum PendingValue { get; set; }

        protected List<Enum> mValidTokens { get; } = new List<Enum>();
        public IReadOnlyList<Enum> ValidTokens => mValidTokens;

        /// <summary>
        /// Whether there is a specific value in this setting.
        /// </summary>
        public override bool HasValue => Value != null;

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

        protected override void Load(string text)
        {
            if(TextSerializers.TryParse(text, out Enum loadedValue))
            {
                DesiredValue = loadedValue;
                PendingValue = loadedValue;
                TrySetToPending();
            }
        }

        protected override void Save(XmlWriter writer)
        {
            // Convert the enum value to a string and write it to the stream writer.
            string uniqueName = this.GetSettingUniqueId();

            writer.WriteStartElement(uniqueName);
            writer.WriteString(TextSerializers.ToText(Value));
            writer.WriteEndElement();
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
            DesiredValue = DefaultValue;
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
                    {
                        PendingValue = null;
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

            foreach (TEnum token in Utils.GetTokens(typeof(TEnum)))
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
