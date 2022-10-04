using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings
{
    public static class SettingsSystem
    {
        private static List<SettingBase> mSettings { get; set; } = new List<SettingBase>();
        private static bool Finalized { get; set; } = false;

        public static void RegisterSetting(SettingBase setting)
        {
            if(!Finalized)
            {
                mSettings.Add(setting);
            }
            else
            {
                LTrace.Assert(false, "Cannot add settings once the system is finalized.");
            }
        }

        public static void FinalizeSettingSystem()
        {
            foreach (SettingBase setting in mSettings)
            {
                setting.FinalizeSetting();
            }

            Dictionary<SettingBase, bool> checkedSettings = new();
            Stack<SettingBase> clashStack = new();

            // Check for circular dependencies.
            foreach(SettingBase setting in mSettings)
            {
                if(CheckForCircularDependenciesOnSetting(setting, checkedSettings, clashStack))
                {
                    LTrace.Assert(false, $"Circular dependency detected: {String.Join(",", clashStack)}");
                }
            }

            Finalized = true;
        }

        private static bool CheckForCircularDependenciesOnSetting(SettingBase setting, 
            Dictionary<SettingBase, bool> checkedSettings, Stack<SettingBase> clashStack)
        {
            if(checkedSettings.TryGetValue(setting, out bool value))
            {
                return value;
            }

            if(clashStack.Contains(setting))
            {
                checkedSettings[setting] = true;
                return true;
            }

            clashStack.Push(setting);

            foreach(SettingBase dependentSetting in setting.Dependents)
            {
                if(CheckForCircularDependenciesOnSetting(dependentSetting, checkedSettings, clashStack))
                {
                    return true;
                }
            }

            clashStack.Pop();
            checkedSettings[setting] = false;
            return false;
        }
    }
}
