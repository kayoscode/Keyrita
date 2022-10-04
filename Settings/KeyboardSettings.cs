using Keyrita.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Settings
{
    public enum eKeyboardShape
    {
        [UIData("ANSI")]
        ANSI,
        [UIData("ISO")]
        ISO,
        [UIData("JIS")]
        JIS,
    }

    /// <summary>
    /// Readonly interface to keyboard settings.
    /// </summary>
    public interface IKeyboardSettings
    {
        IEnumValueSetting KeyboardShape { get; }
    }

    public interface IMeasurementSettings
    {
        IOnOffSetting ShowAnnotations { get; }
    }

    /// <summary>
    /// The shape of the keyboard setting.
    /// </summary>
    public class KeyboardShapeSetting : EnumValueSetting<eKeyboardShape>
    {
        public KeyboardShapeSetting()
            : base("Keyboard Shape", eKeyboardShape.ANSI, eSettingAttributes.None)
        {
        }
    }

    /// <summary>
    /// Whether or not to show measurement annotations for the selected measurement if available.
    /// </summary>
    public class KeyboardShowAnnotations : OnOffSetting
    {
        public KeyboardShowAnnotations() : 
            base("Remove Ansi", eOnOff.Off, eSettingAttributes.None)
        {
        }
    }
}
