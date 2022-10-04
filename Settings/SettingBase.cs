using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings
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
    /// Base class for all settings to derive from.
    /// Settings are designed to automatically update when one of
    /// the settings they depend upon's values change.
    /// </summary>
    public abstract class SettingBase : ISetting
    {
        public INotifyPropertyChanged mValueChanged;

        protected delegate void SettingAction();

        public IReadOnlyList<SettingBase> Dependents => mDependents;
        private List<SettingBase> mDependents = new List<SettingBase>();
        private eSettingAttributes mAttributes = eSettingAttributes.None;
        private string mSettingName;

        public void AddDependent(SettingBase setting)
        {
            mDependents.Add(setting);
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="settingName">The name of the setting for logs.</param>
        /// <param name="attributes">Attributes affecting the behavior of the setting.</param>
        public SettingBase(string settingName,
            eSettingAttributes attributes)
        {
            this.mAttributes = attributes;
            this.mSettingName = settingName;

            SettingsSystem.RegisterSetting(this);
        }

        public abstract bool HasValue { get; protected set; }

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
        public void FinalizeSetting()
        {
            if (!mFinalized)
            {
                SetDependencies();
                SetToDefault();
                mFinalized = true;
            }
            else
            {
                LTrace.Assert(false, $"Setting {mSettingName} has already been finalized");
            }
        }
        private bool mFinalized = false;

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
                // Perform the seting action.
                action();

                // Notify dependents if we changed.
                if (ValueHasChanged)
                {
                    // Notify GUIs.
                    mValueChanged.

                    LTrace.LogInfo($"{mSettingName}: {description}");

                    foreach (SettingBase dependent in mDependents)
                    {
                        dependent.ChangeLimits();
                        dependent.SetToNewLimits();
                        dependent.TrySetToPending();
                    }
                }
            }
            catch (Exception e)
            {
                LTrace.LogError("A serious error has occurred in a setting transaction");
            }

            LTrace.LogInfo($"{mSettingName}");
        }
    }
}
