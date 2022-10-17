using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class SetProgressBar : UserControlBase
    {
        public SetProgressBar()
        {
            InitializeComponent();
        }

        private void SettingUpdated(object changedSetting)
        {
            SyncWithSetting();
        }

        async private void SyncWithSetting()
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));

            while (await periodicTimer.WaitForNextTickAsync())
            {
                if (Setting.IsRunning)
                {
                    mControlBlock.Visibility = Visibility.Visible;
                    mProgressBar.Value = Setting.Progress * 100;
                    mProgressBarText.Text = "Loading " + Setting.SettingName + $"({mProgressBar.Value.ToString("###")}%)";
                }
                else
                {
                    mControlBlock.Visibility = Visibility.Collapsed;
                    periodicTimer.Dispose();
                }
            }
        }

        private static readonly DependencyProperty SettingProperty =
            DependencyProperty.Register(nameof(Setting),
                                        typeof(ProgressSetting),
                                        typeof(SetProgressBar),
                                        new PropertyMetadata(OnSettingChanged));

        private static void OnSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as SetProgressBar;
            control.UpdateSetting(e.NewValue as ProgressSetting);
        }

        private void UpdateSetting(ProgressSetting newValue)
        {
            // Unregister setting change listeners.
            if (mSetting != null)
            {
                mSetting.NotifyProgressBarStarted.Remove(SettingUpdated);
            }

            mSetting = newValue;

            if(mSetting != null)
            {
                mSetting.NotifyProgressBarStarted.AddGui(SettingUpdated);
                SyncWithSetting();
            }
        }

        /// <summary>
        /// The setting which this control is linked to.
        /// </summary>
        public ProgressSetting Setting
        {
            get
            {
                return mSetting;
            }
            set
            {
                SetValue(SettingProperty, value);
            }
        }

        private ProgressSetting mSetting;

        protected override void OnClose()
        {
            Setting = null;
        }
    }
}
