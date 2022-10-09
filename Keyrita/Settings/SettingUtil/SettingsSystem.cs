using System;
using Keyrita.Util;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Keyrita.Settings.SettingUtil
{
    public class UndoRedoState<T>
    {
        private const int MAX_UNDO_COUNT = 250;
        private IList<T> ValueStack { get; set; } = new List<T>(MAX_UNDO_COUNT);

        private int Top { get; set; } = 1;
        private int Base { get; set; } = 0;
        private int Current { get; set; } = 0;

        public bool IsEmpty => (Top == Base) && (Base == Current) && (Current == 0);

        public UndoRedoState()
        {
            // Start with all null entries.
            for(int i = 0; i < MAX_UNDO_COUNT; i++)
            {
                ValueStack.Add(default(T));
            }
        }

        /// <summary>
        /// Adds a copy of the setting's current value to the Top of the stack
        /// The Current value should be pointing at the item we just added.
        /// So should top.
        /// </summary>
        /// <param name="value"></param>
        public void UpdateValue(T value)
        {
            // Deal with overflow.
            // This is a circular stack.
            Current += 1;
            Current = Current > MAX_UNDO_COUNT - 1 ? 0 : Current;
            Top = Current;

            if(Top == Base)
            {
                Base += 1;
                Base = Base > MAX_UNDO_COUNT - 1 ? 0 : Base;
            }

            ValueStack[Current] = value;
        }

        /// <summary>
        ///      -> Current
        /// Base -> Starting state.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryUndo(out T value)
        {
            value = default(T);

            var tempCurrent = Current - 1;
            tempCurrent = tempCurrent < 0 ? MAX_UNDO_COUNT - 1 : tempCurrent;

            if(tempCurrent != Base)
            {
                Current = tempCurrent;
                value = (T)ValueStack[Current];

                return true;
            }

            return false;
        }

        public bool TryRedo(out T value)
        {
            value = default(T);

            if (Current != Top)
            {
                Current += 1;
                Current = Current > MAX_UNDO_COUNT - 1 ? 0 : Current;

                value = (T)ValueStack[Current];

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Class handling settings and setting transactions.
    /// </summary>
    public static class SettingsSystem
    {
        /// <summary>
        /// Stores the setting states for undo/redo of settings.
        /// </summary>
        private static UndoRedoState<string> mUndoRedoState = new UndoRedoState<string>();

        private static List<SettingBase> mSettings { get; } = new List<SettingBase>();
        private static Dictionary<string, SettingBase> mSettingsByUid { get; } = new();

        private static bool Finalized { get; set; } = false;

        private static readonly string SettingXMLNode = "Settings";

        public static void SaveUndoState()
        {
            StringWriter undoRedoXml;
            using (undoRedoXml = new StringWriter())
            {
                using(XmlWriter writer = XmlWriter.Create(undoRedoXml))
                {
                    SaveSettings(writer, true);
                }
            }

            string text = undoRedoXml.ToString();
            mUndoRedoState.UpdateValue(text);
        }

        public static void Undo()
        {
            if(mUndoRedoState.TryUndo(out string xml))
            {
                XmlDocument settings = new XmlDocument();
                settings.LoadXml(xml);

                LoadSettings(settings, true);
            }
        }

        public static void Redo()
        {
            if(mUndoRedoState.TryRedo(out string xml))
            {
                XmlDocument settings = new XmlDocument();
                settings.LoadXml(xml);

                LoadSettings(settings, true);
            }
        }

        /// <summary>
        /// Saves all settings to an XML file.
        /// </summary>
        /// <param name="fileStream"></param>
        public static void SaveSettings(XmlWriter xmlWriter, bool undoredo)
        {
            LTrace.Assert(Finalized, "Cannot save before finalized.");

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement(SettingXMLNode);

            // Allow each setting to write to the file.
            foreach(SettingBase setting in mSettings)
            {
                setting.SaveToFile(xmlWriter, undoredo);
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
        }

        public static void LoadSettings(XmlDocument xmlReader, bool undoredo)
        {
            LTrace.Assert(Finalized, "Cannot load before finalized.");

            XmlNode settingNode = xmlReader.SelectSingleNode(SettingXMLNode);
            XmlNodeList settings = settingNode.ChildNodes;

            foreach(XmlNode setting in settings)
            {
                var uid = setting.Name;

                if(mSettingsByUid.TryGetValue(uid, out SettingBase settingToload))
                {
                    settingToload.LoadFromfile(setting.InnerXml, undoredo);
                }
            }

            // In graph order, set each setting to the loaded value.
            OperateInGraphOrder((setting) =>
            {
                setting.SetToDesiredValue();
            });
        }

        /// <summary>
        /// Sets each setting to their default value.
        /// </summary>
        public static void DefaultSettings()
        {
            OperateInGraphOrder((setting) =>
            {
                setting.SetToDefault();
            });
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
            OperateInGraphOrder((setting) =>
            {
                setting.SetToDefault();
            });

            Finalized = true;

            // Save the default state into the undo list.
            SaveUndoState();
        }

        private delegate void GraphOrderOperation(SettingBase setting);

        private static void OperateInGraphOrder(GraphOrderOperation operation)
        {
            Dictionary<SettingBase, bool> resolvedDependents = new();
            Dictionary<SettingBase, bool> resolvedDependencies = new();

            foreach (SettingBase setting in mSettings)
            {
                // Make sure all the dependent settings are resolved before we initialize this one.
                ResolveDependents(setting, resolvedDependents, operation);
                ResolveDependencies(setting, resolvedDependencies);
            }
        }

        private static void ResolveDependents(SettingBase setting,
            Dictionary<SettingBase, bool> checkedSettings,
            GraphOrderOperation operation)
        {
            if (checkedSettings.TryGetValue(setting, out bool value))
            {
                return;
            }

            foreach (SettingBase dependent in setting.Dependents)
            {
                ResolveDependents(dependent, checkedSettings, operation);
            }

            try
            {
                operation(setting);
            }
            catch(Exception)
            {
                LTrace.Assert(false, "An error occurred in graph order execution.");
            }

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
