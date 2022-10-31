using System;
using System.Collections.Generic;
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

        protected void HandleCharSwap(byte ch1, byte ch2, List<(int, int)> si,
            int k1i, int k1j, int k2i, int k2j)
        {
            var kb = mKbState.TransformedKbState;
            var bgFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            for(int k = 0; k < si.Count; k++)
            {
                var otherKeyPos = si[k];
                var otherKey = kb[otherKeyPos.Item1][otherKeyPos.Item2];
                mResult.PerKeyResult[k1i, k1j] -= (bgFreq[otherKey, ch1] / totalBg);
                mResult.PerKeyResult[k1i, k1j] += (bgFreq[otherKey, ch2] / totalBg);

                // Handle the inverse.
                mResult.PerKeyResult[otherKeyPos.Item1, otherKeyPos.Item2] -= (bgFreq[ch1, otherKey] / totalBg);
                mResult.PerKeyResult[otherKeyPos.Item1, otherKeyPos.Item2] += (bgFreq[ch2, otherKey] / totalBg);
            }
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // Only need to update the scissors total. Which means subtract the previous scissors from the new one.
            // NOTE: the keyboard state has already changed, so subtract the indices from the previous position instead of the new position.
            var kb = mKbState.TransformedKbState;

            var bgFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            var scissorIndices = SettingState.KeyboardSettings.ScissorMap;

            var ch1 = kb[k2i][k2j];
            var ch2 = kb[k1i][k1j];

            // The scissor map includes the scissor indices going into that key.
            // The scissor map is its own inverse, so update the inverse positions as well.
            var si1 = scissorIndices.GetScissorsAt(k1i, k1j);
            var si2 = scissorIndices.GetScissorsAt(k2i, k2j);

            HandleCharSwap(ch1, ch2, si1, k1i, k1j, k2i, k2j);
            HandleCharSwap(ch2, ch1, si2, k2i, k2j, k1i, k1j);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private TransformedKbStateResult mKbState;
        private KeyToFingerAsIntResult mK2f;

        protected override void Compute()
        {
            mResult = new ScissorsResult(NodeId);
            mKbState = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var kb = mKbState.TransformedKbState;

            mK2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var keyToFinger = mK2f.KeyToFinger;

            var bgFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            var totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            var scissorIndices = SettingState.KeyboardSettings.ScissorMap;

            // We only care about the top and bottoms rows. Scissors will obviously be much lower for the pinkies. But
            // that does mean more common keys might go in those slots
            for (int i = 0; i < KeyboardStateSetting.ROWS; i++) 
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    long scissorTotal = 0;
                    var si = scissorIndices.GetScissorsAt(i, j);
                    var finger = keyToFinger[i][j];
                    var hand = FingerUtil.GetHandForFingerAsInt(finger);

                    for(int k = 0; k < si.Count; k++)
                    {
                        scissorTotal += bgFreq[kb[si[k].Item1][si[k].Item2], kb[i][j]];
                    }

                    mResult.TotalResult += scissorTotal;
                    mResult.PerFingerResult[finger] += scissorTotal;
                    mResult.PerHandResult[(int)hand] += scissorTotal;

                    // Set the value for this key and normalize.
                    mResult.PerKeyResult[i, j] = scissorTotal;
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
