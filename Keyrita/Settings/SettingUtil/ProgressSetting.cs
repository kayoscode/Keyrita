using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings.SettingUtil
{
    /// <summary>
    /// Read only interface to a progress setting.
    /// </summary>
    public interface IProgressSetting
    {
        double Progress { get; }
        bool IsRunning { get; } 
    }

    /// <summary>
    /// Setting which reports information about progress.
    /// </summary>
    public abstract class ProgressSetting : SettingBase, IProgressSetting
    {
        protected ProgressSetting(string settingName, eSettingAttributes attributes, Enum sInstance = null) 
            : base(settingName, attributes, sInstance)
        {
        }

        public ChangeNotification NotifyProgressBarStarted = new ChangeNotification();
        public ChangeNotification NotifyCanceled = new ChangeNotification();

        public abstract double Progress { get; }
        public abstract bool IsRunning { get; }
        public abstract void Cancel();
    }
}
