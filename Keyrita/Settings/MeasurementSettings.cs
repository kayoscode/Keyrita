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

        public AddMeasurementAction(eMeasurements measurement)
            : base(measurement.UIText(), measurement)
        {
        }

        protected override void DoAction()
        {
            SettingState.MeasurementSettings.InstalledMeasurements[Meas].TurnMeasOn();
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
            if (this.IsOn)
            {
                OperationSystem.InstallOp(this.SInstance);
            }
            else
            {
                OperationSystem.UninstallOp(this.SInstance);
            }
        }

        public void TurnMeasOn()
        {
            this.mValidTokens.Clear();
            this.mValidTokens.Add(eOnOff.On);
            this.SetToNewLimits();
        }

        public void TurnMeasOff()
        {
            this.mValidTokens.Clear();
            this.mValidTokens.Add(eOnOff.Off);
            this.SetToNewLimits();
        }
    }
}
