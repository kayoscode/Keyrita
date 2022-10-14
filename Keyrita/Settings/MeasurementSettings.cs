using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyrita.Gui;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Settings
{
    /// <summary>
    /// Enumerates all the user-facing measurements which can be done.
    /// </summary>
    public enum eMeasurements
    {
        [UIData("SFB")]
        SameFingerBigram,
        [UIData("Bad SFB")]
        BadSameFingerBigrams,
        [UIData("SFT")]
        SameFingerTrigram,
        [UIData("SFS")]
        SameFingerSkipgrams,
        [UIData("Inrolls")]
        InRolls,
        [UIData("Outrolls")]
        OutRolls,
        [UIData("Alternations")]
        Alternations,
        [UIData("Left Hand Usage")]
        LeftHandBalance,
        [UIData("Right Hand Usage")]
        RightHandBalance,
        [UIData("Redirects")]
        Redirects,
        [UIData("Bad Redirects")]
        BadRedirects,
        [UIData("Finger Usage")]
        FingerUsage,
    }

    /// <summary>
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
        }
    }
}
