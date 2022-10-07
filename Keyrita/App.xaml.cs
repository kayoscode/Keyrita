using Keyrita.Settings.SettingUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create settings.
            SettingState.Init();

            // After all settings are initialized, it's time to finalize the setting system.
            SettingsSystem.FinalizeSettingSystem();
        }
    }
}
