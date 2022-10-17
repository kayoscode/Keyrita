using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Xml.XPath;
using Keyrita.Gui;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Enumerates all the user-facing measurements which can be done.
    /// </summary>
    public enum eMeasurements
    {
        [UIData("SFB", "Shows the bigrams which use the same finger")]
        SameFingerBigram,
        [UIData("SFS", "Shows the skipgrams which use the same finger")]
        SameFingerSkipgrams,
        [UIData("Rolls", "Shows in/out rolls")]
        Rolls,
        [UIData("Alternations", "Shows the alternation rate")]
        Alternations,
        [UIData("Hand Usage", "Shows the hand balance")]
        HandBalance,
        [UIData("Redirects", "Shows the redirection rate")]
        Redirects,
        [UIData("Finger Usage", "Shows finger balance stats")]
        FingerUsage,
    }

    /// <summary>
    /// Base class which all measurement operations will use.
    /// </summary>
    public abstract class MeasurementOp 
        : OperationBase
    {
        protected enum eGoodChangeType
        {
            Up,
            Down
        }

        protected MeasurementOp(Enum id) 
            : base(id)
        {
        }

        #region Measurement Display Contract

        public abstract uint NumUICols { get; }
        public abstract string UIRowName(uint rowIdx);
        public abstract double UIRowValue(uint rowIdx);
        public abstract Brush UIRowColor(uint rowIdx);

        protected static readonly Brush GoodChangeBrush = Brushes.Green;
        protected static readonly Brush BadChangeBrush = Brushes.Red;
        protected static readonly Brush NeutralChangeBrush  = Brushes.GhostWhite;
        protected eGoodChangeType GoodChangeType { get; set; } = eGoodChangeType.Down;

        #endregion
    }

    /// <summary>
    /// Lists measurements with independent stats for fingers, hands, and total
    /// </summary>
    public abstract class FingerHandMeasurement : MeasurementOp
    {
        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="id"></param>
        protected FingerHandMeasurement(Enum id) 
            : base(id)
        {
        }

        private const int NUM_RESULTS = 11;
        private double[] mResults = new double[NUM_RESULTS];
        private Brush[] mResultColors = new Brush[NUM_RESULTS]
        {
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
            NeutralChangeBrush,
        };

        private static Dictionary<eFinger, uint> FINGER_TO_ROW = new Dictionary<eFinger, uint>() 
        {
            {eFinger.LeftPinkie, 3 },
            {eFinger.LeftRing, 4 },
            {eFinger.LeftMiddle, 5 },
            {eFinger.LeftIndex, 6 },
            {eFinger.RightIndex, 7 },
            {eFinger.RightMiddle, 8 },
            {eFinger.RightRing, 9 },
            {eFinger.RightPinkie, 10 }
        };

        private void SetResult(uint index, double result)
        {
            double roundedResult = Math.Round(result, 2, MidpointRounding.AwayFromZero);
            double absoluteDifference = Math.Abs(roundedResult - mResults[index]);

            if (absoluteDifference <= .001 && absoluteDifference >= -.001)
            {
                mResultColors[index] = NeutralChangeBrush;
            }
            else if(roundedResult > mResults[index])
            {
                mResultColors[index] = GoodChangeType == eGoodChangeType.Up? GoodChangeBrush : BadChangeBrush;
            }
            else if(roundedResult < mResults[index])
            {
                mResultColors[index] = GoodChangeType == eGoodChangeType.Down? GoodChangeBrush : BadChangeBrush;
            }

            mResults[index] = roundedResult;
        }

        protected void SetTotalResult(double result)
        {
            SetResult(0, result);
        }

        protected void SetLeftHandResult(double result)
        {
            SetResult(1, result);
        }

        protected void SetRightHandResult(double result)
        {
            SetResult(2, result);
        }

        protected void SetFingerResult(eFinger finger, double result)
        {
            LTrace.Assert(finger != eFinger.LeftThumb, "Cannot set value for thumb");
            LTrace.Assert(finger != eFinger.RightThumb, "Cannot set value for thumb");
            SetResult(FINGER_TO_ROW[finger], result);
        }

        public override string UIRowName(uint rowIdx)
        {
            switch (rowIdx)
            {
                case 0: return "Total";
                case 1: return "LH";
                case 2: return "RH";
                default:
                    eFinger finger = eFinger.None;
                    
                    foreach(var i in FINGER_TO_ROW)
                    {
                        if(i.Value == rowIdx)
                        {
                            finger = i.Key;
                            break;
                        }
                    }

                    return finger.UIAbbreviation();
            }
        }

        public override double UIRowValue(uint rowIdx)
        {
            return mResults[rowIdx];
        }

        public override Brush UIRowColor(uint rowIdx)
        {
            return mResultColors[rowIdx];
        }

        /// <summary>
        /// 8 Fingers and two hands.
        /// Hands are the first two, and fingers are the second 8.
        /// One for the total
        /// </summary>
        public override uint NumUICols => NUM_RESULTS;
    }
}
