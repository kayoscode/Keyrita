﻿using Keyrita.Serialization;
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
        protected ConcreteValueSetting(string settingName,
                             T defaultValue,
                             eSettingAttributes attributes,
                             Enum instance = null)
            : base(settingName, attributes, instance)
        {
            mDefaultValue = defaultValue;
        }

        public override bool HasValue => Value != null;

        public T Value => mValue;
        protected T mValue;

        protected T DesiredValue { get; set; }

        public virtual T DefaultValue => mDefaultValue;
        protected T mDefaultValue;

        protected T mPendingValue;
        protected T mLimitValue;

        protected override bool ValueHasChanged => !Equals(mPendingValue, Value);

        protected override void Load(string text)
        {
            if(TextSerializers.TryParse(text, out T value)) 
            {
                DesiredValue = value;
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

        public override void SetToDesiredValue()
        {
            mPendingValue = DesiredValue;
            TrySetToPending();
        }

        protected override void SetToDefault()
        {
            DesiredValue = mPendingValue;
            SetToDesiredValue();
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
