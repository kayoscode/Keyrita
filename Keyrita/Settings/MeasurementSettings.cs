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
        public AddMeasurementAction(eMeasurements measurement)
            : base(measurement.UIText(), measurement)
        {
        }

        protected override void DoAction()
        {
            OperationSystem.InstallOp(this.SInstance);
        }
    }
}
