using Keyrita.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Keyrita.Settings.SettingUtil
{
    /// <summary>
    /// Readonly interface to a concrete value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConcreteValue<T> : ISetting
    {
        T Value { get; }
        T DefaultValue { get; }
    }

    /// <summary>
    /// Setting storing a concrete value with an T type.
    /// </summary>
    public abstract class ConcreteValueSetting<T> : SettingBase, IConcreteValue<T>
    {
        public ConcreteValueSetting(string settingName,
                             T defaultValue,
                             eSettingAttributes attributes)
            : base(settingName, attributes)
        {
            mDefaultValue = defaultValue;
        }

        public override bool HasValue => Value != null;

        public T Value => mValue;
        protected T mValue;

        public virtual T DefaultValue => mDefaultValue;
        protected T mDefaultValue;

        protected T mPendingValue;
        protected T mLimitValue;

        protected override bool ValueHasChanged => !Equals(mPendingValue, Value);

        protected override void Load(string text)
        {
            if(TextSerializers.TryParse(text, out T value)) 
            {
                mPendingValue = value;
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
            mPendingValue = mDefaultValue;
            TrySetToPending();
        }

        protected override void SetToNewLimits()
        {
            mPendingValue = mLimitValue;
            TrySetToPending();
        }

        protected override void TrySetToPending()
        {
            if(mPendingValue == null)
            {
                SettingTransaction("Setting to null", () =>
                {
                    mValue = mPendingValue;
                });
            }
            else if (!mPendingValue.Equals(Value))
            {
                string description = $"Changing concrete value {mValue} to {mPendingValue}";

                SettingTransaction(description, () =>
                {
                    mValue = mPendingValue;
                });
            }
        }
    }
}
