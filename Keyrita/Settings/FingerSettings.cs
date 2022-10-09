using Keyrita.Settings.SettingUtil;

namespace Keyrita.Settings
{
    /// <summary>
    /// Each finger is given an enum token.
    /// </summary>
    public enum eFinger
    {
        None,
        LeftPinkie = 0,
        LeftRing = 1,
        LeftMiddle = 2,
        LeftIndex = 3,
        LeftThumb = 4,

        RightThumb = 5,
        RightIndex = 6,
        RightMiddle = 7,
        RightRing = 8,
        RightPinkie = 9,
    }

    /// <summary>
    /// The finger's starting position specified per key.
    /// If the key doesn't have a finger on the starting position, it should be set to none.
    /// </summary>
    public class FingerHomePosition : PerkeySetting<eFinger>
    {
        public FingerHomePosition() 
            : base("Finger Home Position", eSettingAttributes.Recall)
        {
        }

        public override void SetToDefault()
        {
            // Just standard homerow qwerty.
            for(int i = 0; i < ROWS; i++)
            {
                for(int j = 0; j < COLS; j++)
                {
                    mDesiredKeyState[i, j] = eFinger.None;
                }
            }

            mDesiredKeyState[1, 0] = eFinger.LeftPinkie;
            mDesiredKeyState[1, 1] = eFinger.LeftRing;
            mDesiredKeyState[1, 2] = eFinger.LeftMiddle;
            mDesiredKeyState[1, 3] = eFinger.LeftIndex;

            // Thumbs are always on the space bar, and we will always make that assumption.
            
            mDesiredKeyState[1, 6] = eFinger.RightIndex;
            mDesiredKeyState[1, 7] = eFinger.RightMiddle;
            mDesiredKeyState[1, 8] = eFinger.RightRing;
            mDesiredKeyState[1, 9] = eFinger.RightPinkie;

            SetToDesiredValue();
        }
    }

    /// <summary>
    /// Setting value derived from the home position setting.
    /// Each finger will be used to hit the key its closest to.
    /// </summary>
    public class FingerMappings : PerkeySetting<eFinger>
    {
        public FingerMappings() 
            : base("Key to Finger Mappings", eSettingAttributes.None)
        {
        }

        protected override void SetDependencies()
        {

        }

        public override void SetToDefault()
        {
            // No default value.
        }
    }
}
