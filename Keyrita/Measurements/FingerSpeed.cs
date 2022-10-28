using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Measurements
{
    public class FingerSpeedResult : SFBResult
    {
        public FingerSpeedResult(Enum resultId) 
            : base(resultId)
        {
        }
    }

    public class FingerSpeed : FingerHandMeasurement
    {
        protected FingerSpeedResult mResult;

        public FingerSpeed() : base(eMeasurements.SameFingerSkipgrams)
        {
            AddInputNode(eInputNodes.SameFingerStats);
            mResult = new FingerSpeedResult(NodeId);
        }

        protected override void Compute()
        {
            var sameFingerStats = (SameFingerStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.SameFingerStats];
            double totalSg2Hits = (double)SettingState.MeasurementSettings.CharFrequencyData.Skipgram2HitCount;

            mResult.TotalResult = sameFingerStats.TotalSfs / totalSg2Hits * 100;

            int resultIdx = 0;
            foreach (eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerSfs = ((double)sameFingerStats.SfsPerFinger[resultIdx] / totalSg2Hits) * 100;
                mResult.PerFingerResult[resultIdx] = fingerSfs;
                SetFingerResult(finger, fingerSfs);

                resultIdx++;
            }

            mResult.PerHandResult[(int)eHand.Left] = sameFingerStats.SfsPerHand[(int)eHand.Left] / totalSg2Hits * 100;
            mResult.PerHandResult[(int)eHand.Right] = sameFingerStats.SfsPerHand[(int)eHand.Right] / totalSg2Hits * 100;

            SetLeftHandResult(mResult.PerHandResult[(int)eHand.Left]);
            SetRightHandResult(mResult.PerHandResult[(int)eHand.Right]);
            SetTotalResult(mResult.TotalResult);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }
}
