using System;
using System.Linq;
using Keyrita.Measurements;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Meas
{
    public class FingerBalanceResult : AnalysisResult
    {
        public FingerBalanceResult(Enum measToken)
            :base(measToken)
        {
        }

        public double LeftHandUsage { get; set; }
        public double RightHandUsage { get; set; }
        public double[] PerFingerResult { get; private set; } = new double[Utils.GetTokens<eFinger>().Count()];
    }

    public class FingerBalance : FingerHandMeasurement
    {
        private FingerBalanceResult mResult;
        public FingerBalance() : base(eMeasurements.FingerBalance)
        {
            mResult = new FingerBalanceResult(this.NodeId);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TransformedKbStateResult kbState = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var kb = kbState.TransformedKbState;
            
            TransformedCharacterToFingerAsIntResult c2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            var charToFinger = c2f.CharacterToFinger;

            uint[] charFreq = SettingState.MeasurementSettings.CharFrequencyData.CharFreq;

            long leftHandUsage = 0;
            long rightHandUsage = 0;
            long[] fingerUsage = new long[Utils.GetTokens<eFinger>().Count()];
            long totalChars = SettingState.MeasurementSettings.CharFrequencyData.CharHitCount;

            // NOTE: were intentionally ignoring space here.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    byte character = kb[i][j];
                    int finger = charToFinger[character];
                    eHand hand = FingerUtil.GetHandForFingerAsInt(finger);

                    if(hand == eHand.Left)
                    {
                        leftHandUsage += charFreq[character];
                    }
                    else if(hand == eHand.Right)
                    {
                        rightHandUsage += charFreq[character];
                    }
                    else
                    {
                        LogUtils.Assert(false, "Key was not assigned a hand.");
                    }

                    fingerUsage[finger] += charFreq[character];
                }
            }

            long total = leftHandUsage + rightHandUsage;
            SetLeftHandResult(leftHandUsage / (double)totalChars * 100);
            SetRightHandResult(rightHandUsage / (double)totalChars * 100);

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerSfbs = ((double)fingerUsage[resultIdx] / (double)totalChars) * 100;
                mResult.PerFingerResult[resultIdx] = fingerSfbs;
                SetFingerResult(finger, fingerSfbs);

                resultIdx++;
            }

            SetTotalResult(total / (double)totalChars * 100);
        }
    }
}
