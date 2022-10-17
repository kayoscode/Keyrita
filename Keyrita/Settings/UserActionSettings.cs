using System.Runtime.CompilerServices;
using Keyrita.Gui;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Settings
{
    public enum eKeyboardReflectDirection
    {
        [UIData("Horizontal", "Reflects each key across the Y axis")]
        Horizontal,
        [UIData("Vertical", "Reflects each key across the X axis")]
        Vertical,
        [UIData("Both", "Reflects each key across the X and Y axis")]
        Both
    }

    /// <summary>
    /// Triggers the keyboard to reflect along the horizontal or vertical axis.
    /// </summary>
    public class UserActionReflect : ActionSetting
    {
        protected eKeyboardReflectDirection Dir => (eKeyboardReflectDirection)this.SInstance;

        public UserActionReflect(eKeyboardReflectDirection rd)
           : base(rd.UIText(), rd)
        {
        }

        public override string ToolTip
        {
            get
            {
                return Dir.UIToolTip();
            }
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
