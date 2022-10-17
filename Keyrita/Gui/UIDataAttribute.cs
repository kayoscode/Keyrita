namespace Keyrita.Gui
{
    /// <summary>
    /// An attribute describing ui element data.
    /// </summary>
    public class UIDataAttribute : System.Attribute
    {
        public string UIText;
        public string Abbreviation;
        public string ToolTip;

        public UIDataAttribute(string UIText, string toolTip, string abbreviation)
        {
            this.UIText = UIText;
            this.Abbreviation = abbreviation;
            this.ToolTip = toolTip;
        }

        public UIDataAttribute(string UIText, string toolTip)
        {
            this.UIText = UIText;
            this.Abbreviation = UIText;
            this.ToolTip = toolTip;
        }

        public UIDataAttribute(string UIText)
        {
            this.UIText = UIText;
            this.Abbreviation = this.UIText;
            this.ToolTip = null;
        }
    }
}
