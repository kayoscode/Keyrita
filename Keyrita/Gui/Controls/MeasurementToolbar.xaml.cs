using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Keyrita.Measurements;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for MeasurementToolbar.xaml
    /// </summary>
    public partial class MeasurementToolbar : UserControlBase
    {
        public MeasurementToolbar()
        {
            InitializeComponent();

            mAvailableMeasurements = SettingState.MeasurementSettings.AvailableMeasurements;
            mAvailableMeasurements.ValueChangedNotifications.AddGui(SyncWithAvailableMeasurements);
            SyncWithAvailableMeasurements(null);
        }

        protected void SyncWithAvailableMeasurements(SettingBase changedSetting)
        {
            mUserMeasurements.Children.Clear();

            var sortedMeasurements = mAvailableMeasurements.Collection.ToList();
            sortedMeasurements.Sort();

            foreach (eMeasurements measurement in sortedMeasurements) 
            {
                ActionButton newMeasurement = new ActionButton();
                newMeasurement.Action = SettingState.UserActions.AddMeasurements[measurement];
                newMeasurement.Margin = new Thickness(2, 2, 2, 2);
                newMeasurement.Height = 50;

                mUserMeasurements.Children.Add(newMeasurement);
            }
        }

        protected ElementSetSetting<eMeasurements> mAvailableMeasurements;

        protected override void OnClose()
        {
            mAvailableMeasurements.ValueChangedNotifications.Remove(SyncWithAvailableMeasurements);
        }
    }
}
