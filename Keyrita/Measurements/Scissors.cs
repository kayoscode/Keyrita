using System;
using System.Linq;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Gives the number of scissors per hand and finger, and total.
    /// </summary>
    public class ScissorsResult : PerKeyAnalysisResult
    {
        public ScissorsResult(Enum resultId) : base(resultId)
        {
        }

        public double TotalResult { get; set; }
        public double[] PerHandResult { get; private set; } = new double[Utils.GetTokens<eHand>().Count()];
        public double[] PerFingerResult { get; private set; } = new double[Utils.GetTokens<eFinger>().Count()];
    }

    /// <summary>
    /// A scissor for a given key is when you press that key and either go to 
    /// </summary>
    public class Scissors : FingerHandMeasurement
    {
        protected ScissorsResult mResult;
        public Scissors() : base(eMeasurements.Scissors)
        {
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            mResult = new ScissorsResult(NodeId);
            var kbState = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var kb = kbState.TransformedKbState;

            var k2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var keyToFinger = k2f.KeyToFinger;

            var bgFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            var totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            // We only care about the top and bottoms rows. Scissors will obviously be much lower for the pinkies. But
            // that does mean more common keys might go in those slots
            for(int i = 0; i < KeyboardStateSetting.ROWS; i += 2)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    long scissorTotal = 0;

                    // Generate scissor positions.
                    int scissory = 0;
                    if(i == 0)
                    {
                        scissory = 2;
                    }

                    int scissorx1 = j - 1;
                    int scissorx2 = j + 1;

                    // Handle left scissor. Note: This is intentionally the amount of scissors COMING to that key, not from it.
                    if(scissorx1 >= 0)
                    {
                        scissorTotal += bgFreq[kb[scissory][scissorx1], kb[i][j]];
                    }
                    // Handle the right scissor.
                    if (scissorx2 < KeyboardStateSetting.COLS)
                    {
                        scissorTotal += bgFreq[kb[scissory][scissorx2], kb[i][j]];
                    }

                    var finger = keyToFinger[i][j];
                    var hand = FingerUtil.GetHandForFingerAsInt(finger);
                    mResult.TotalResult += scissorTotal;
                    mResult.PerFingerResult[finger] += scissorTotal;
                    mResult.PerHandResult[(int)hand] += scissorTotal;

                    // Set the value for this key and normalize.
                    mResult.PerKeyResult[i, j] = scissorTotal / (double)totalBg;
                }
            }

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerResult = ((double)mResult.PerFingerResult[resultIdx] / totalBg) * 100;
                mResult.PerFingerResult[resultIdx] = fingerResult;
                SetFingerResult(finger, mResult.PerFingerResult[resultIdx]);

                resultIdx++;
            }

            mResult.TotalResult = mResult.TotalResult / totalBg * 100;
            mResult.PerHandResult[(int)eHand.Left] = mResult.PerHandResult[(int)eHand.Left] / totalBg * 100;
            mResult.PerHandResult[(int)eHand.Right] = mResult.PerHandResult[(int)eHand.Right] / totalBg * 100;

            SetLeftHandResult(mResult.PerHandResult[(int)eHand.Left]);
            SetRightHandResult(mResult.PerHandResult[(int)eHand.Right]);
            SetTotalResult(mResult.TotalResult);
        }
    }
}
