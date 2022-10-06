using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using System.Xml;

namespace Keyrita.Settings.SettingUtil
{
    /// <summary>
    /// A setting which doesn't store any state, but it performs an action.
    /// Mostly used for button click events.
    /// Accomplishes this by storing a boolean under the hood that gets updated every time the limits are changed.
    /// Then the effect is called subsequently.
    /// </summary>
    public abstract class ActionSetting : SettingBase
    {
        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="sInstanceId"></param>
        public ActionSetting(string settingName, Enum sInstanceId = null)
            : base(settingName, eSettingAttributes.None, sInstanceId)
        {
        }

        protected bool LimitValue { get; set; }
        protected bool Value { get; set; }
        protected bool PendingValue { get; set; }

        public override bool HasValue => false;
        protected override bool ValueHasChanged => PendingValue != Value;

        protected override sealed void Load(string text)
        {
            // Do nothing, there's no state.
        }

        protected override sealed void Save(XmlWriter writer)
        {
            // Do nothing, there's no state.
        }

        /// <summary>
        /// Swap to other state to trigger a setting change + effect.
        /// </summary>
        protected override sealed void ChangeLimits()
        {
            LimitValue = !Value;
        }

        /// <summary>
        /// Manually triggers the event.
        /// </summary>
        public void Trigger()
        {
            Action();
        }

        /// <summary>
        /// Implementers must implement Effect. Action is generic behavior.
        /// </summary>
        protected override sealed void Action()
        {
            DoAction();
        }

        /// <summary>
        /// Each action setting has an effect that takes place the moment the limits are updated.
        /// </summary>
        protected abstract void DoAction();

        protected override sealed void SetToDefault()
        {
            Value = false;
        }

        protected override sealed void SetToNewLimits()
        {
            PendingValue = LimitValue;
            TrySetToPending();
        }

        protected override sealed void TrySetToPending()
        {
            if (Value != PendingValue)
            {
                string description = "Doing effect";
                SettingTransaction(description, () =>
                {
                    Value = PendingValue;
                });
            }
        }
    }
}
