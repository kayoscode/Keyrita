using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings
{
    public enum eKeyboardShape
    {
        ANSI,
        ISO,
        JIS,
    }

    /// <summary>
    /// Readonly interface to keyboard settings.
    /// </summary>
    public interface IKeyboardSettings
    {
        IEnumValueSetting<eKeyboardShape> KeyboardShape { get; }
    }

    /// <summary>
    /// The shape of the keyboard setting.
    /// </summary>
    public class KeyboardShapeSetting : EnumValueSetting<eKeyboardShape>
    {
        public KeyboardShapeSetting(string settingName)
            : base(settingName, eKeyboardShape.ANSI, eSettingAttributes.None)
        {
        }

        protected override void SetDependencies()
        {
        }
    }
}
