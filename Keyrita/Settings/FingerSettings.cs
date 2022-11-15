using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Keyrita.Gui;
using Keyrita.Serialization;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

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

    public enum eHand : int
    {
        None,
        Left,
        Right
    }

    /// <summary>
    /// Useful functions on fingers.
    /// </summary>
    public static class FingerUtil
    {
        private static eHand[] mFingerToHand = new eHand[]
        {
            eHand.None,

            eHand.Left,
            eHand.Left,
            eHand.Left,
            eHand.Left,
            eHand.Left,

            eHand.Right,
            eHand.Right,
            eHand.Right,
            eHand.Right,
            eHand.Right,
        };

        public static double[,,,] MovementDistance =
            new double[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS, KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];

        static FingerUtil()
        {
            for (int startx = 0; startx < 3; startx++)
            {
                for (int starty = 0; starty < 10; starty++)
                {
                    for (int endx = 0; endx < 3; endx++)
                    {
                        for (int endy = 0; endy < 10; endy++)
                        {
                            double x_dist = (endx - startx);
                            double y_dist = (endy - starty);
                            double distance = Math.Pow(x_dist * x_dist + y_dist * y_dist, .65);
                            MovementDistance[startx, starty, endx, endy] = distance;
                        }
                    }
                }
            }
        }

        public static eHand GetHandForFingerAsInt(int finger)
        {
            return mFingerToHand[finger];
        }

        public static eHand GetOtherHand(eHand hand)
        {
            LogUtils.Assert(hand != eHand.None);

            return hand == eHand.Left ? eHand.Right : eHand.Left;
        }
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
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
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

        protected override void Init()
        {
            SettingState.FingerSettings.FingerHomePosition.AddDependent(this);
        }

        protected override void ChangeLimits(eFinger[,] values)
        {
            // Assuming each key is in a perfect grid should get us the right results.
            var fhs = SettingState.FingerSettings.FingerHomePosition;

            Dictionary<eFinger, (int row, int col)> fingerPositions = new Dictionary<eFinger, (int row, int col)>();
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    eFinger finger = fhs.GetValueAt(i, j);
                    if (finger != eFinger.None)
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

                        foreach (var kvp in fingerPositions)
                        {
                            double dr = (i - kvp.Value.row);
                            double dc = (j - kvp.Value.col);
                            double distance = Math.Sqrt(dr * dr + dc * dc);

                            if (distance < minFingerDistance)
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

    public class EffortMapSetting : PerkeySetting<double>
    {
        /// <summary>
        /// Weight applied for using each key, copied from oxelizer :D
        /// </summary>
        private static double[][] KEY_LOCATION_PENALTY = new double[KeyboardStateSetting.ROWS][]
        {
            new double[KeyboardStateSetting.COLS]
            {
                3.0, 2.4, 2.0, 2.2, 2.4,  3.3, 2.2, 2.0, 2.4, 3.0,
            },
            new double[KeyboardStateSetting.COLS]
            {
                1.8, 1.3, 1.1, 1.0, 2.6,  2.6, 1.0, 1.1, 1.3, 1.8,
            },
            new double[KeyboardStateSetting.COLS]
            {
                3.5, 3.0, 2.7, 2.2, 3.7,  2.2, 1.8, 2.4, 2.8, 3.3
            }
        };

        public EffortMapSetting() : base("Effort Map", eSettingAttributes.None)
        {
        }

        public double MaxEffort { get; private set; }

        protected void NormalizeEffortMap(double maxValue)
        {
            MaxEffort = 0;

            for(int i = 0; i < mKeyState.GetLength(0); i++)
            {
                for(int j = 0; j < mKeyState.GetLength(1); j++)
                {
                    double normalized = (mDesiredKeyState[i, j] / maxValue) * maxValue;
                    mDesiredKeyState[i, j] = normalized;
                    MaxEffort += normalized;
                }
            }
        }

        public override void SetToDefault()
        {
            for(int i = 0; i < KEY_LOCATION_PENALTY.Length; i++)
            {
                for(int j = 0; j < KEY_LOCATION_PENALTY[i].Length; j++)
                {
                    mDesiredKeyState[i, j] = KEY_LOCATION_PENALTY[i][j];
                }
            }

            NormalizeEffortMap(3.0);
            this.SetToDesiredValue();
        }
    }

    /// <summary>
    /// Weights describing the penalty for having an same finger grams on each finger.
    /// </summary>
    public class FingerWeightsSetting : PerFingerSetting<double>
    {
        public FingerWeightsSetting()
            : base("Finger Speed Weights", eSettingAttributes.None)
        {
        }

        public override void SetToDefault()
        {
            mDesiredState[(int)eFinger.None] = 0;
            mDesiredState[(int)eFinger.LeftPinkie] = 2.4;
            mDesiredState[(int)eFinger.LeftRing] = 1.5;
            mDesiredState[(int)eFinger.LeftMiddle] = 1.15;
            mDesiredState[(int)eFinger.LeftIndex] = 1;

            mDesiredState[(int)eFinger.RightIndex] = 1;
            mDesiredState[(int)eFinger.RightMiddle] = 1.15;
            mDesiredState[(int)eFinger.RightRing] = 1.5;
            mDesiredState[(int)eFinger.RightPinkie] = 2.4;

            SetToDesiredValue();
        }
    }

    /// <summary>
    /// A class to define values of a specific type per finger.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PerFingerSetting<T> : SettingBase
    {
        public static int FINGER_COUNT { get; } = Utils.GetTokens<eFinger>().Count();

        public PerFingerSetting(string settingName, eSettingAttributes attributes) :
            base(settingName, attributes)
        {
        }

        protected T[] mState = new T[FINGER_COUNT];
        protected T[] mNewState = new T[FINGER_COUNT];
        protected T[] mPendingState = new T[FINGER_COUNT];
        protected T[] mDesiredState = new T[FINGER_COUNT];

        public override bool HasValue => mState != null;
        protected override bool ValueHasChanged => StateMatches(mPendingState, mState) > 0;

        protected override void Load(string text)
        {
            string[] state = text.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            int index = 0;

            foreach (string s in state)
            {
                if (TextSerializers.TryParse(s, out T nextCharacter))
                {
                    mDesiredState[index] = nextCharacter;
                }

                index++;
            }
        }

        protected override void Save(XmlWriter writer)
        {
            // Convert the enum value to a string and write it to the stream writer.
            for (int i = 0; i < FINGER_COUNT; i++)
            {
                writer.WriteString(TextSerializers.ToText(mState[i]));
                writer.WriteString(" ");
            }
        }

        protected override void Action()
        {
        }

        /// <summary>
        /// Returns the character at a specified index.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public T GetValueAt(eFinger finger)
        {
            return mState[(int)finger];
        }

        public T GetValueAt(int finger)
        {
            return mState[finger];
        }

        protected override sealed void ModifyLimits()
        {
            ChangeLimits(mNewState);
        }

        protected virtual void ChangeLimits(T[] values)
        {
        }

        /// <summary>
        /// Sets the keyboard layout to desired.
        /// </summary>
        public override sealed void SetToDesiredValue()
        {
            CopyState(mPendingState, mDesiredState);
            TrySetToPending();
        }


        protected override sealed void SetToNewLimits()
        {
            // Copy the pending board to the new layout.
            CopyState(mPendingState, mNewState);
            TrySetToPending();
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            // If the pending keyboard state does not match the current keyboard state, start a setting transaction.
            var count = StateMatches(mPendingState, mState);

            if (count != 0)
            {
                var description = $"Changing {count} keys";

                InitiateSettingChange(description, userInitiated, () =>
                {
                    CopyState(mState, mPendingState);
                });
            }
        }

        /// <summary>
        /// Copies state 2 to state 1
        /// </summary>
        /// <param name="kb1"></param>
        /// <param name="kb2"></param>
        protected static void CopyState(T[] s1, T[] s2)
        {
            for (int i = 0; i < FINGER_COUNT; i++)
            {
                s1[i] = s2[i];
            }
        }

        /// <summary>
        /// Returns whether two states are the same
        /// Specifically returns the number of items that don't match.
        /// </summary>
        /// <param name="kb1"></param>
        /// <param name="kb2"></param>
        /// <returns></returns>
        public static int StateMatches(T[] s1, T[] s2)
        {
            var count = 0;

            for (int i = 0; i < FINGER_COUNT; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(s1[i], s2[i]))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
