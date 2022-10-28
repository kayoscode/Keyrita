
using System;
using System.Linq;
using Keyrita.Measurements;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Meas
{
    public class HomeUsageResult : AnalysisResult
    {
        public HomeUsageResult(Enum measToken)
            :base(measToken)
        {
        }

        public double LeftHandUsage { get; set; }
        public double RightHandUsage { get; set; }
        public double[] PerFingerResult { get; private set; } = new double[Utils.GetTokens<eFinger>().Count()];
    }

    public class HomeUsage : FingerHandMeasurement
    {
        private HomeUsageResult mResult;
        public HomeUsage() : base(eMeasurements.HomeRowUsage)
        {
            mResult = new HomeUsageResult(this.NodeId);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);
            AddInputNode(eInputNodes.FingerAsIntToHomePosition);
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

            FingerAsIntToHomePositionResult f2h = (FingerAsIntToHomePositionResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.FingerAsIntToHomePosition];
            var fingerToHome = f2h.FingerToHomePosition;

            uint[] charFreq = SettingState.MeasurementSettings.CharFrequencyData.CharFreq;

            long leftHandHomeUsage = 0;
            long rightHandHomeUsage = 0;
            long[] fingerHomeUsage = new long[Utils.GetTokens<eFinger>().Count()];
            long totalChars = SettingState.MeasurementSettings.CharFrequencyData.CharHitCount;

            // NOTE: were intentionally ignoring space here.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    byte character = kb[i][j];
                    int finger = charToFinger[character];
                    eHand hand = FingerUtil.GetHandForFingerAsInt(finger);

                    // Did the finger have to leave its homerow?
                    (int, int) startPos = fingerToHome[finger];

                    if (i == startPos.Item1)
                    {
                        if(hand == eHand.Left)
                        {
                            leftHandHomeUsage += charFreq[character];
                        }
                        else if(hand == eHand.Right)
                        {
                            rightHandHomeUsage += charFreq[character];
                        }
                        else
                        {
                            LogUtils.Assert(false, "Key was not assigned a hand.");
                        }

                        fingerHomeUsage[finger] += charFreq[character];
                    }
                }
            }

            long total = leftHandHomeUsage + rightHandHomeUsage;
            SetLeftHandResult(leftHandHomeUsage / (double)totalChars * 100);
            SetRightHandResult(rightHandHomeUsage / (double)totalChars * 100);

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerSfbs = ((double)fingerHomeUsage[resultIdx] / (double)totalChars) * 100;
                mResult.PerFingerResult[resultIdx] = fingerSfbs;
                SetFingerResult(finger, fingerSfbs);

                resultIdx++;
            }

            SetTotalResult(total / (double)totalChars * 100);
        }
    }
}
