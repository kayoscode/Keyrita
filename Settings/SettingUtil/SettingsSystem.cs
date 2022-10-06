using Keyrita.Util;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Keyrita.Settings.SettingUtil
{
    public static class SettingsSystem
    {
        private static List<SettingBase> mSettings { get; } = new List<SettingBase>();
        private static Dictionary<string, SettingBase> mSettingsByUid { get; } = new();

        private static bool Finalized { get; set; } = false;

        private static readonly string SettingXMLNode = "Settings";

        /// <summary>
        /// Saves all settings to an XML file.
        /// </summary>
        /// <param name="fileStream"></param>
        public static void SaveSettings(XmlWriter xmlWriter)
        {
            if (Finalized)
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement(SettingXMLNode);

                // Allow each setting to write to the file.
                foreach(SettingBase setting in mSettings)
                {
                    setting.SaveToFile(xmlWriter);
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
            else
            {
                LTrace.Assert(false, "Settings system must be initialized before saving");
            }
        }

        public static void LoadSettings(XmlDocument xmlReader)
        {
            if (Finalized)
            {
                XmlNode settingNode = xmlReader.SelectSingleNode(SettingXMLNode);
                XmlNodeList settings = settingNode.ChildNodes;

                foreach(XmlNode setting in settings)
                {
                    var uid = setting.Name;

                    if(mSettingsByUid.TryGetValue(uid, out SettingBase settingToload))
                    {
                        settingToload.LoadFromfile(setting.InnerText);
                    }
                }
            }
            else
            {
                LTrace.Assert(false, "Settings system must be initialized before loading");
            }
        }

        public static void RegisterSetting(SettingBase setting)
        {
            if (!Finalized)
            {
                mSettings.Add(setting);

                string uid = setting.GetSettingUniqueId();

                LTrace.Assert(!mSettingsByUid.ContainsKey(uid), $"Two settings share the same UID: {uid}");
                mSettingsByUid[uid] = setting;
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
                setting.PreInitialization();
            }

            Dictionary<SettingBase, bool> checkedSettings = new();
            Stack<SettingBase> clashStack = new();

            // Check for circular dependencies.
            foreach (SettingBase setting in mSettings)
            {
                if (CheckForCircularDependenciesOnSetting(setting, checkedSettings, clashStack))
                {
                    LTrace.Assert(false, $"Circular dependency detected: {string.Join(",", clashStack)}");
                }
            }

            // Starting with the lowest dependents, resolve first value
            // Check for circular dependencies.
            checkedSettings.Clear();

            Dictionary<SettingBase, bool> resolvedDependents = new();
            Dictionary<SettingBase, bool> resolvedDependencies = new();

            foreach (SettingBase setting in mSettings)
            {
                // Make sure all the dependent settings are resolved before we initialize this one.
                ResolveDependents(setting, resolvedDependents);
                ResolveDependencies(setting, resolvedDependencies);
            }

            Finalized = true;
        }

        private static void ResolveDependents(SettingBase setting,
            Dictionary<SettingBase, bool> checkedSettings)
        {
            if (checkedSettings.TryGetValue(setting, out bool value))
            {
                return;
            }

            foreach (SettingBase dependent in setting.Dependents)
            {
                ResolveDependents(dependent, checkedSettings);
            }

            setting.FinalizeSetting();

            checkedSettings[setting] = true;
        }

        private static void ResolveDependencies(SettingBase setting,
            Dictionary<SettingBase, bool> checkedSettings)
        {
            if (checkedSettings.TryGetValue(setting, out bool value))
            {
                return;
            }

            foreach (SettingBase dependency in setting.Dependencies)
            {
                ResolveDependencies(dependency, checkedSettings);
            }

            checkedSettings[setting] = true;
        }

        private static bool CheckForCircularDependenciesOnSetting(SettingBase setting,
            Dictionary<SettingBase, bool> checkedSettings, Stack<SettingBase> clashStack)
        {
            if (checkedSettings.TryGetValue(setting, out bool value))
            {
                return value;
            }

            if (clashStack.Contains(setting))
            {
                checkedSettings[setting] = true;
                return true;
            }

            clashStack.Push(setting);

            foreach (SettingBase dependentSetting in setting.Dependents)
            {
                if (CheckForCircularDependenciesOnSetting(dependentSetting, checkedSettings, clashStack))
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
