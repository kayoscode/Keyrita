using Keyrita.Util;
using System;
using System.Collections.Generic;

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
        // If one of the settings it depends on's value changes and
        // the desired value is no longer valid, if those limitations are 
        // removed, the value should bounce back to it.
        BounceBack = 2,
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
        public delegate void ChangeNotif(SettingBase settingChanged);
        protected List<ChangeNotif> Notifications = new List<ChangeNotif>();

        public void AddGui(ChangeNotif notificationFunc)
        {
            Notifications.Add(notificationFunc);
        }

        public void Remove(ChangeNotif changeNotif)
        {
            Notifications.Remove(changeNotif);
        }

        public void NotifyGui(SettingBase setting)
        {
            foreach (var gui in Notifications)
            {
                try
                {
                    gui(setting);
                }
                catch (Exception e)
                {
                    LTrace.LogError("The gui encountered a seriuos error processing a change notification.");
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
            eSettingAttributes attributes)
        {
            mAttributes = attributes;
            mSettingName = settingName;

            SettingsSystem.RegisterSetting(this);
        }

        public abstract bool HasValue { get; }

        /// <summary>
        /// This is where you target specific settings in which you want to know if they change.
        /// By convention, every setting registered here should be used in the limit changing function.
        /// </summary>
        protected virtual void SetDependencies()
        {
        }

        /// <summary>
        /// Does final initialization (post construction).
        /// </summary>
        public virtual void PreInitialization()
        {
            SetDependencies();
        }

        /// <summary>
        /// Finally set the value to default, and handle pending/desired values.
        /// </summary>
        public virtual void FinalizeSetting()
        {
            SetToDefault();
        }

        /// <summary>
        /// When one of the values of a dependent changes, 
        /// use this function to modify the limits for this setting.
        /// </summary>
        protected abstract void ChangeLimits();

        /// <summary>
        /// When the value of this setting changes, make changes in the system.
        /// </summary>
        protected abstract void Action();

        /// <summary>
        /// Writes the data for this setting to the file.
        /// </summary>
        public abstract void Save();

        /// <summary>
        /// Consumes text and fills the proper value from the input.
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// True if the value has been modified.
        /// </summary>
        protected abstract bool ValueHasChanged { get; }

        /// <summary>
        /// After the limits have been modified, we have to adjust
        /// the setting value to fit those limits. A setting transaction should be called 
        /// when this occurs.
        /// </summary>
        protected abstract void SetToNewLimits();

        /// <summary>
        /// Sets the next value to the default.
        /// </summary>
        protected abstract void SetToDefault();

        /// <summary>
        /// If the setting transaction succeeds, this is called.
        /// The value should be updated here.
        /// </summary>
        protected abstract void TrySetToPending();

        /// <summary>
        /// Genericly handle setting changes.
        /// </summary>
        protected void SettingTransaction(string description, SettingAction action)
        {
            try
            {
                // Notify dependents if we changed.
                if (ValueHasChanged)
                {
                    // Perform the seting action.
                    action();

                    // Notify GUIs.
                    ValueChangedNotifications.NotifyGui(this);

                    LTrace.LogInfo($"{mSettingName}: {description}");

                    foreach (SettingBase dependent in mDependents)
                    {
                        dependent.ChangeLimits();
                        dependent.SetToNewLimits();
                        dependent.LimitsChangedNotifications.NotifyGui(dependent);
                    }

                    // Perform a generic action when the value of a setting changes.
                    Action();
                }
            }
            catch (Exception e)
            {
                LTrace.LogError("A serious error has occurred in a setting transaction");
            }
        }
    }
}
