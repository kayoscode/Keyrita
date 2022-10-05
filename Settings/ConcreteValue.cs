using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings
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
    /// Setting storing a concrete value with an object type.
    /// </summary>
    public abstract class ConcreteValueSetting : SettingBase, IConcreteValue<object>
    {
        public ConcreteValueSetting(string settingName, 
                             object defaultValue,
                             eSettingAttributes attributes)
            :base(settingName, attributes)
        {
            this.mDefaultValue = DefaultValue;
        }

        public override bool HasValue => Value != null;

        public object Value => mValue;
        protected object mValue;

        public virtual object DefaultValue => mDefaultValue;
        protected object mDefaultValue;

        protected object mPendingValue;
        protected object mLimitValue;

        protected override bool ValueHasChanged => !object.Equals(mPendingValue, Value);

        public override void Load()
        {
        }

        public override void Save()
        {
        }

        protected override void Action()
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
            if (!mPendingValue.Equals(Value))
            {
                string description = $"Changing concrete value {mValue} to {mPendingValue}";

                SettingTransaction(description, () =>
                {
                    mValue = mPendingValue;
                });
            }
        }
    }

    /// <summary>
    /// Stores a concrete value with a generic type.
    /// Please don't set the underlying data to the incorrect data type. it wont end well.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ConcreteValueSetting<T> : ConcreteValueSetting, IConcreteValue<T>
    {
        public ConcreteValueSetting(string settingName, T defaultValue, eSettingAttributes attributes)
            : base(settingName, defaultValue, attributes)
        {
        }

        T IConcreteValue<T>.Value => (T)base.Value;
        T IConcreteValue<T>.DefaultValue => (T)base.DefaultValue;
    }
}
