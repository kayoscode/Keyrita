using System;
using System.Collections.Generic;
using Keyrita.Analysis.AnalysisUtil;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for Measurements.xaml
    /// </summary>
    public partial class MeasurementOutput : UserControlBase
    {
        protected Dictionary<Enum, MeasurementLine> mMeasurementOnOffStates = new Dictionary<Enum, MeasurementLine>();

        public MeasurementOutput()
        {
            InitializeComponent();
            
            foreach(SettingBase meas in SettingState.MeasurementSettings.InstalledPerFingerMeasurements.Values)
            {
                meas.ValueChangedNotifications.Add(SyncWithInstalledMeasurements);

                var measLine = new MeasurementLine();
                measLine.ParentGrid = mLineGrid;
                mMeasLines.Children.Add(measLine);
                mMeasurementOnOffStates[meas.SInstance] = measLine;
                measLine.Visibility = System.Windows.Visibility.Collapsed;

                SyncWithInstalledMeasurements(meas);
            }
        }

        protected void SyncWithInstalledMeasurements(object settingChange)
        {
            var changedSetting = settingChange as OnOffSetting;

            if(changedSetting != null && changedSetting.IsOn)
            {
                mMeasurementOnOffStates[((SettingBase)settingChange).SInstance].Measurement = changedSetting.SInstance;
            }
            else
            {
                mMeasurementOnOffStates[((SettingBase)settingChange).SInstance].Measurement = null;
            }
        }

        protected override void OnClose()
        {
            foreach (SettingBase meas in SettingState.MeasurementSettings.InstalledPerFingerMeasurements.Values)
            {
                meas.ValueChangedNotifications.Remove(SyncWithInstalledMeasurements);
            }
        }
    }
}
