using System;
using System.Collections.Generic;
using Keyrita.Gui;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Settings
{
    /// <summary>
    /// Each finger is given an enum token.
    /// </summary>
    public enum eFinger : int
    {
        [UIData("None")]
        None,
        [UIData("Left Pinkie", null, "LPink")]
        LeftPinkie,
        [UIData("Left Ring", null, "LRing")]
        LeftRing,
        [UIData("Left Middle", null, "LMid")]
        LeftMiddle,
        [UIData("Left Index", null, "LInd")]
        LeftIndex,
        [UIData("Left Thumb", null, "LThmb")]
        LeftThumb,

        [UIData("Right Thumb", null, "RThmb")]
        RightThumb,
        [UIData("Right Index", null, "RInd")]
        RightIndex,
        [UIData("Right Middle", null, "RMid")]
        RightMiddle,
        [UIData("Right Ring", null, "RRing")]
        RightRing,
        [UIData("Right Pinkie", null, "RPink")]
        RightPinkie,
    }

    /// <summary>
    /// The finger's starting position specified per key.
    /// If the key doesn't have a finger on the starting position, it should be set to none.
    /// </summary>
    public class FingerHomePositionSetting : PerkeySetting<eFinger>
    {
        public FingerHomePositionSetting() 
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
    public class KeyMappingSetting : PerkeySetting<eFinger>
    {
        public KeyMappingSetting() 
            : base("Key to Finger Mappings", eSettingAttributes.None)
        {
        }

        protected override void SetDependencies()
        {
            SettingState.FingerSettings.FingerHomePosition.AddDependent(this);
        }

        protected override void ChangeLimits(eFinger[,] values)
        {
            // Assuming each key is in a perfect grid should get us the right results.
            var fhs = SettingState.FingerSettings.FingerHomePosition;

            Dictionary<eFinger, (int row, int col)> fingerPositions = new Dictionary<eFinger, (int row, int col)>();
            for(int i = 0; i < ROWS; i++)
            {
                for(int j = 0; j < COLS; j++)
                {
                    eFinger finger = fhs.GetValueAt(i, j);
                    if(finger != eFinger.None)
                    {
                        fingerPositions[finger] = (i, j);
                    }
                }
            }

            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    eFinger finger = fhs.GetValueAt(i, j);

                    if (finger != eFinger.None)
                    {
                        values[i, j] = finger;
                    }
                    else
                    {
                        eFinger minFinger = eFinger.None;
                        double minFingerDistance = 1000;

                        foreach(var kvp in fingerPositions)
                        {
                            double dr = (i - kvp.Value.row);
                            double dc = (j - kvp.Value.col);
                            double distance = Math.Sqrt(dr * dr + dc * dc);

                            if(distance < minFingerDistance)
                            {
                                minFingerDistance = distance;
                                minFinger = kvp.Key;
                            }
                        }

                        values[i, j] = minFinger;
                    }
                }
            }

            CopyBoard(mDesiredKeyState, mNewKeyState);
        }

        public override void SetToDefault()
        {
            // No default value.
        }
    }
}
