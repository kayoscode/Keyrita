using System;
using System.Linq;
using Keyrita.Measurements;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Settings
{
    /// The set of measurements available to the user.
    /// Nonrecallable.
    /// </summary>
    public class AvailableMeasurementList : ElementSetSetting<eMeasurements>
    {
        public AvailableMeasurementList() 
            : base("Available Measurements", eSettingAttributes.None)
        {
            mDefaultCollection = Utils.GetTokens<eMeasurements>().ToHashSet<eMeasurements>();
        }
    }

    /// <summary>
    /// Action which adds a measurement to the system.
    /// </summary>
    public class AddMeasurementAction : ActionSetting
    {
        protected eMeasurements Meas => (eMeasurements)this.SInstance;

        public override string ToolTip
        {
            get
            {
                return this.SInstance.UIToolTip();
            }
        }

        public AddMeasurementAction(eMeasurements measurement)
            : base(measurement.UIText(), measurement)
        {
        }

        protected override void DoAction()
        {
            if (SettingState.MeasurementSettings.InstalledPerFingerMeasurements.ContainsKey(Meas))
            {
                SettingState.MeasurementSettings.InstalledPerFingerMeasurements[Meas].TurnMeasOn();
            }
            else if (SettingState.MeasurementSettings.InstalledDynamicMeasurements.ContainsKey(Meas))
            {
                SettingState.MeasurementSettings.InstalledDynamicMeasurements[Meas].TurnMeasOn();
            }
            else
            {
                LogUtils.Assert(false, "Unknown measurement type.");
            }
        }
    }

    /// <summary>
    /// On if the measurement is installed, off otherwise.
    /// </summary>
    public class MeasurementInstalledSetting : OnOffSetting
    {
        public MeasurementInstalledSetting(eMeasurements measurement)
            : base($"Measurement OnOff State", eOnOff.Off, eSettingAttributes.Recall, measurement)
        {
        }

        protected override void Action()
        {
            if(this.IsOn)
            {
                AnalysisGraphSystem.InstallNode(this.SInstance);
            }
            else
            {
                AnalysisGraphSystem.RemoveNode(this.SInstance);
            }
        }

        public void TurnMeasOn()
        {
            this.PendingValue = eOnOff.On;
            this.TrySetToPending(true);
        }

        public void TurnMeasOff()
        {
            this.PendingValue = eOnOff.Off;
            this.TrySetToPending(true);
        }
    }

    /// <summary>
    /// The number of trigrams to use for computations.
    /// </summary>
    public class TrigramDepthSetting : ConcreteValueSetting<int>
    {
        public TrigramDepthSetting() : 
            base("Trigram Depth", 2500, eSettingAttributes.None)
        {
        }
    }
}
