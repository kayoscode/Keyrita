using System.Runtime.CompilerServices;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Settings
{
    public enum eKeyboardReflectDirection
    {
        Horizontal,
        Vertical,
        Both
    }

    /// <summary>
    /// Triggers the keyboard to reflect along the horizontal or vertical axis.
    /// </summary>
    public class UserActionReflect : ActionSetting
    {
        protected eKeyboardReflectDirection Dir => (eKeyboardReflectDirection)this.SInstance;

        public UserActionReflect(eKeyboardReflectDirection rd)
           : base($"Reflect {rd}", rd)
        {
        }

        protected override void DoAction()
        {
            // Take the current layout and reflect it in a set direction.
            if (Dir == eKeyboardReflectDirection.Horizontal)
            {
                SettingState.KeyboardSettings.KeyboardState.ReflectHorz();
            }
            else if(Dir == eKeyboardReflectDirection.Vertical)
            {
                SettingState.KeyboardSettings.KeyboardState.ReflectVert();
            }
            else
            {
                SettingState.KeyboardSettings.KeyboardState.ReflectHorz();
                SettingState.KeyboardSettings.KeyboardState.ReflectVert();
            }
        }
    }
}
