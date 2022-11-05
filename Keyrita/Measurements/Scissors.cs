using System;
using System.Collections.Generic;
using System.Linq;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Measurements
{
    public class UserFacingScissorsResult : AnalysisResult
    {
        public UserFacingScissorsResult(Enum resultId) : base(resultId)
        {
        }

        public double TotalResult { get; set; }
        public double[] PerHandResult { get; private set; } = new double[Utils.GetTokens<eHand>().Count()];
        public double[] PerFingerResult { get; private set; } = new double[Utils.GetTokens<eFinger>().Count()];
    }

    public class UserFacingScissors : FingerHandMeasurement
    {
        private UserFacingScissorsResult mResult;
        public UserFacingScissors() : base(eMeasurements.Scissors)
        {
            AddInputNode(eInputNodes.ScissorsIntermediate);
            mResult = new UserFacingScissorsResult(this.NodeId);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            double bgTotal = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            var mScissorsResult = (ScissorsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.ScissorsIntermediate];
            mResult.TotalResult = mScissorsResult.TotalResult / bgTotal * 100;

            // Set meas results.
            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                mResult.PerFingerResult[(int)finger] = mScissorsResult.PerFingerResult[(int)finger] / bgTotal * 100;
                SetFingerResult(finger, mResult.PerFingerResult[(int)finger]);
                resultIdx++;
            }

            mResult.PerHandResult[(int)eHand.Left] = mScissorsResult.PerHandResult[(int)eHand.Left] / bgTotal * 100;
            mResult.PerHandResult[(int)eHand.Right] = mScissorsResult.PerHandResult[(int)eHand.Right] / bgTotal * 100;
            SetLeftHandResult(mResult.PerHandResult[(int)eHand.Left]);
            SetRightHandResult(mResult.PerHandResult[(int)eHand.Right]);
            SetTotalResult(mResult.TotalResult);
        }
    }

    /// <summary>
    /// Gives the number of scissors per hand and finger, and total.
    /// </summary>
    public class ScissorsResult : AnalysisResult
    {
        public ScissorsResult(Enum resultId) : base(resultId)
        {
        }

        public long TotalResult { get; set; }
        public long[] PerHandResult { get; private set; } = new long[Utils.GetTokens<eHand>().Count()];
        public long[] PerFingerResult { get; private set; } = new long[Utils.GetTokens<eFinger>().Count()];
        public long[][] PerKeyResult { get; private set; } = new long[KeyboardStateSetting.ROWS][]
        {
            new long[KeyboardStateSetting.COLS],
            new long[KeyboardStateSetting.COLS],
            new long[KeyboardStateSetting.COLS]
        };
    }

    /// <summary>
    /// A scissor for a given key is when you press that key and either go to 
    /// </summary>
    public class ScissorsIntermediate : GraphNode
    {
        protected ScissorsResult mResult;
        public ScissorsIntermediate() : base(eInputNodes.ScissorsIntermediate)
        {
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
        }

        protected void HandleCharSwap(byte ch1, byte ch2, List<(int, int)> si,
            int k1i, int k1j)
        {
            var kb = mKbState.TransformedKbState;
            var bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;

            for(int k = 0; k < si.Count; k++)
            {
                var otherKeyPos = si[k];
                var otherCh = kb[otherKeyPos.Item1][otherKeyPos.Item2];
                long subCh1, subCh2, subCh3, subCh4;

                if(otherCh == ch1)
                {
                    otherCh = ch2;
                    subCh1 = bigramFreq[otherCh][ch1];
                    subCh2 = bigramFreq[ch1][otherCh];

                    mResult.TotalResult -= subCh1;
                    mResult.TotalResult += subCh2;

                    mResult.PerKeyResult[k1i][k1j] -= subCh1;
                    mResult.PerKeyResult[k1i][k1j] += subCh2;
                }
                else
                {
                    subCh1 = bigramFreq[otherCh][ch1];
                    subCh2 = bigramFreq[ch1][otherCh];
                    subCh3 = bigramFreq[otherCh][ch2];
                    subCh4 = bigramFreq[ch2][otherCh];

                    mResult.TotalResult -= subCh1;
                    mResult.TotalResult -= subCh2;
                    mResult.TotalResult += subCh3;
                    mResult.TotalResult += subCh4;

                    mResult.PerKeyResult[k1i][k1j] -= subCh1;
                    mResult.PerKeyResult[k1i][k1j] += subCh3;
                    mResult.PerKeyResult[otherKeyPos.Item1][otherKeyPos.Item2] -= subCh2;
                    mResult.PerKeyResult[otherKeyPos.Item1][otherKeyPos.Item2] += subCh4;
                }
            }
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // Only need to update the scissors total. Which means subtract the previous scissors from the new one.
            // NOTE: the keyboard state has already changed, so subtract the indices from the previous position instead of the new position.
            var kb = mKbState.TransformedKbState;
            var scissorIndices = SettingState.KeyboardSettings.ScissorMap;
            var ch1 = kb[k2i][k2j];
            var ch2 = kb[k1i][k1j];

            // The scissor map includes the scissor indices going into that key.
            // The scissor map is its own inverse, so update the inverse positions as well.
            var si1 = scissorIndices.GetScissorsAt(k1i, k1j);
            var si2 = scissorIndices.GetScissorsAt(k2i, k2j);

            HandleCharSwap(ch1, ch2, si1, k1i, k1j);
            HandleCharSwap(ch2, ch1, si2, k2i, k2j);
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
                        scissorTotal += bgFreq[kb[si[k].Item1][si[k].Item2]][kb[i][j]];
                    }

                    mResult.TotalResult += scissorTotal;
                    mResult.PerFingerResult[finger] += scissorTotal;
                    mResult.PerHandResult[(int)hand] += scissorTotal;

                    // Set the value for this key and normalize.
                    mResult.PerKeyResult[i][j] = scissorTotal;
                }
            }
        }
    }
}
