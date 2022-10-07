using System.Web;

namespace Keyrita.Gui
{
    /// <summary>
    /// An attribute describing ui element data.
    /// </summary>
    public class UIDataAttribute : System.Attribute
    {
        public string UIText;
        public string Abbreviation;

        public UIDataAttribute(string UIText, string abbreviation)
        {
            this.UIText = UIText;
            this.Abbreviation = abbreviation;
        }

        public UIDataAttribute(string UIText)
        {
            this.UIText = UIText;
            this.Abbreviation = this.UIText;
        }
    }
}
