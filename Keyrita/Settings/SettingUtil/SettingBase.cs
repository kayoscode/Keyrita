using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Sources;
using System.Windows.Controls;
using System.Xml;

namespace Keyrita.Settings.SettingUtil
{
    /// <summary>
    /// Attributes which can be applied to settings.
    /// </summary>
    [Flags]
    public enum eSettingAttributes
    {
        None = 0,
        // Whether the setting should be exported to a file by default.
        Recall = 1,
        RecallNoUndo,
    }

    /// <summary>
    /// Functions every setting must implement.
    /// </summary>
    public interface ISetting
    {
        void AddDependent(SettingBase setting);
    }

    /// <summary>
    /// Notifies gui elements about changes.
    /// </summary>
    public class ChangeNotification
    {
        public delegate void ChangeNotif(object settingChanged);
        protected List<ChangeNotif> Notifications = new List<ChangeNotif>();

        public void Add(ChangeNotif notificationFunc)
        {
            Notifications.Add(notificationFunc);
        }

        public void Remove(ChangeNotif changeNotif)
        {
            Notifications.Remove(changeNotif);
        }

        public void NotifyGui(object setting)
        {
            foreach (var gui in Notifications)
            {
                try
                {
                    gui(setting);
                }
                catch (Exception)
                {
                    LogUtils.LogError("The gui encountered a seriuos error processing a change notification.");
                }
            }
        }
    }

    /// <summary>
    /// Base class for all settings to derive from.
    /// Settings are designed to automatically update when one of
    /// the settings they depend upon's values change.
    /// </summary>
    public abstract class SettingBase : ISetting
    {
        public ChangeNotification ValueChangedNotifications = new ChangeNotification();
        public ChangeNotification LimitsChangedNotifications = new ChangeNotification();

        protected delegate void SettingAction();

        public IReadOnlyList<SettingBase> Dependents => mDependents;
        private List<SettingBase> mDependents = new List<SettingBase>();
        public IReadOnlyList<SettingBase> Dependencies => mDependencies;
        protected List<SettingBase> mDependencies = new List<SettingBase>();

        private eSettingAttributes mAttributes = eSettingAttributes.None;

        public string SettingName => mSettingName;
        private string mSettingName;

        public Enum SInstance { get; protected set; }

        public void AddDependent(SettingBase setting)
        {
            mDependents.Add(setting);
            setting.mDependencies.Add(this);
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="settingName">The name of the setting for logs.</param>
        /// <param name="attributes">Attributes affecting the behavior of the setting.</param>
        public SettingBase(string settingName,
            eSettingAttributes attributes,
            Enum sInstance = null)
        {
            mAttributes = attributes;
            mSettingName = settingName;
            this.SInstance = sInstance;

            SettingsSystem.RegisterSetting(this);
        }

        /// <summary>
        /// Returns an identifier unique to this setting. This is verified
        /// to be unique by the setting system.
        /// </summary>
        /// <returns></returns>
        public virtual string GetSettingUniqueId()
        {
            // Return the setting name stripped of all spaces, followed by .instance.
            StringBuilder builder = new();

            // Strip setting name of all spaces.
            builder.Append(SettingName.Replace(" ", ""));

            if(SInstance != null)
            {
                builder.Append($".{SInstance}");
            }

            return builder.ToString();
        }

        /// <summary>
        /// If the setting appears in the UI, this returns the text which will be shown.
        /// </summary>
        public virtual string ToolTip
        {
            get
            {
                return null;
            }
        }

        public abstract bool HasValue { get; }

        /// <summary>
        /// This is where you target specific settings in which you want to know if they change.
        /// By convention, every setting registered here should be used in the limit changing function.
        /// </summary>
        protected virtual void Init()
        {
        }

        /// <summary>
        /// Does final initialization (post construction).
        /// </summary>
        public virtual void PreInitialization()
        {
            Init();
        }

        /// <summary>
        /// When one of the values of a dependent changes, 
        /// use this function to modify the limits for this setting.
        /// </summary>
        protected abstract void ModifyLimits();

        /// <summary>
        /// When the value of this setting changes, make changes in the system.
        /// </summary>
        protected abstract void Action();

        public void SaveToFile(XmlWriter writer, bool undoRedo)
        {
            if(!HasValue)
            {
                return;
            }

            if(undoRedo)
            {
                if (mAttributes.HasFlag(eSettingAttributes.Recall))
                {
                    string uniqueId = this.GetSettingUniqueId();
                    writer.WriteStartElement(uniqueId);
                    Save(writer);
                    writer.WriteEndElement();
                }
            }
            else
            {
                if(mAttributes.HasFlag(eSettingAttributes.Recall) ||
                    mAttributes.HasFlag(eSettingAttributes.RecallNoUndo))
                {
                    string uniqueId = this.GetSettingUniqueId();
                    writer.WriteStartElement(uniqueId);
                    Save(writer);
                    writer.WriteEndElement();
                }
            }
        }

        public void LoadFromfile(string text, bool undoRedo)
        {

            if(undoRedo)
            {
                if (mAttributes.HasFlag(eSettingAttributes.Recall))
                {
                    Load(text);
                }
            }
            else
            {
                if(mAttributes.HasFlag(eSettingAttributes.Recall) ||
                    mAttributes.HasFlag(eSettingAttributes.RecallNoUndo))
                {
                    Load(text);
                }
            }
        }

        /// <summary>
        /// Writes the data for this setting to the file.
        /// </summary>
        protected abstract void Save(XmlWriter writer);

        /// <summary>
        /// Consumes text and fills the proper value from the input.
        /// Note: the value should never be loaded directly into the value part of the setting.
        /// </summary>
        protected abstract void Load(string text);

        /// <summary>
        /// True if the value has been modified.
        /// </summary>
        protected abstract bool ValueHasChanged { get; }

        /// <summary>
        /// Whatever the user wants the setting to be, set it to that.
        /// </summary>
        public abstract void SetToDesiredValue();

        /// <summary>
        /// After the limits have been modified, we have to adjust
        /// the setting value to fit those limits. A setting transaction should be called 
        /// when this occurs.
        /// </summary>
        protected abstract void SetToNewLimits();

        /// <summary>
        /// Sets the next value to the default.
        /// </summary>
        public abstract void SetToDefault();

        /// <summary>
        /// If the setting transaction succeeds, this is called.
        /// The value should be updated here.
        /// </summary>
        protected abstract void TrySetToPending(bool userInitiated = false);

        /// <summary>
        /// Genericly handle setting changes.
        /// </summary>
        protected void InitiateSettingChange(string description, bool userInitiated, SettingAction action)
        {
            try
            {
                // Notify dependents if we changed.
                if (ValueHasChanged)
                {
                    // Perform the seting action.
                    action();

                    LogUtils.LogInfo($"{mSettingName}: {description}");

                    foreach (SettingBase dependent in mDependents)
                    {
                        dependent.ModifyLimits();
                        dependent.SetToNewLimits();
                        dependent.LimitsChangedNotifications.NotifyGui(dependent);
                    }

                    // Perform a generic action when the value of a setting changes.
                    Action();

                    // Notify GUIs.
                    ValueChangedNotifications.NotifyGui(this);

                    // Store the previous state for undo/redo
                    if(userInitiated && mAttributes.HasFlag(eSettingAttributes.Recall))
                    {
                        SettingsSystem.SaveUndoState();
                    }
                }
            }
            catch (Exception)
            {
                LogUtils.LogError("A serious error has occurred in a setting transaction");
            }
        }
    }
}
