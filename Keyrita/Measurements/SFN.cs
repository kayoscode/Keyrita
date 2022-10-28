using System;
using System.Collections.Generic;
using System.Linq;
using Keyrita.Interop.NativeAnalysis;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Measurements
{
    public class SFBResult : AnalysisResult
    {
        public SFBResult(Enum resultId) 
            : base(resultId)
        {
        }

        public double TotalResult { get; set; }
        public double[] PerHandResult { get; private set; } = new double[Utils.GetTokens<eHand>().Count()];
        public double[] PerFingerResult { get; private set; } = new double[Utils.GetTokens<eFinger>().Count()];
    }

    public class Sfbs : FingerHandMeasurement
    {
        protected SFBResult mResult;

        public Sfbs() : base(eMeasurements.SameFingerBigram)
        {
            AddInputNode(eInputNodes.SameFingerStats);
            mResult = new SFBResult(this.NodeId);
        }

        protected override void Compute()
        {
            var sfs = (SameFingerStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.SameFingerStats];
            double totalBigramCount = (double)SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            mResult.TotalResult = sfs.TotalSfbs / totalBigramCount * 100;

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerSfbs = ((double)sfs.SfbsPerFinger[resultIdx] / totalBigramCount) * 100;
                mResult.PerFingerResult[resultIdx] = fingerSfbs;
                SetFingerResult(finger, fingerSfbs);

                resultIdx++;
            }

            mResult.PerHandResult[(int)eHand.Left] = sfs.SfbsPerHand[(int)eHand.Left] / totalBigramCount * 100;
            mResult.PerHandResult[(int)eHand.Right] = sfs.SfbsPerHand[(int)eHand.Right] / totalBigramCount * 100;

            SetLeftHandResult(mResult.PerHandResult[(int)eHand.Left]);
            SetRightHandResult(mResult.PerHandResult[(int)eHand.Right]);
            SetTotalResult(mResult.TotalResult);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }


    public class SFSResult : SFBResult
    {
        public SFSResult(Enum resultId) 
            : base(resultId)
        {
        }
    }

    public class Sfss : FingerHandMeasurement
    {
        protected SFSResult mResult;

        public Sfss() : base(eMeasurements.SameFingerSkipgrams)
        {
            AddInputNode(eInputNodes.SameFingerStats);
            mResult = new SFSResult(NodeId);
        }

        protected override void Compute()
        {
            var sameFingerStats = (SameFingerStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.SameFingerStats];
            double totalSg2Hits = (double)SettingState.MeasurementSettings.CharFrequencyData.Skipgram2HitCount;

            mResult.TotalResult = sameFingerStats.TotalSfs / totalSg2Hits * 100;

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
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
