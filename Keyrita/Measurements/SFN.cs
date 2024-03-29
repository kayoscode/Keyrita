﻿using System;
using System.Collections.Generic;
using System.Linq;
using Keyrita.Interop.NativeAnalysis;
using Keyrita.Analysis;
using Keyrita.Analysis.AnalysisUtil;
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

        public Sfbs(AnalysisGraph graph) : base(eMeasurements.SameFingerBigram, graph)
        {
            AddInputNode(eInputNodes.TwoFingerStats);
            mResult = new SFBResult(this.NodeId);
        }

        protected override void Compute()
        {
            var tfs = (TwoFingerStatsResult)AnalysisGraph.ResolvedNodes[eInputNodes.TwoFingerStats];
            double totalBigramCount = (double)SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            mResult.TotalResult = tfs.TotalSfbs / totalBigramCount * 100;

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerSfbs = ((double)tfs.SfbsPerFinger[resultIdx] / totalBigramCount) * 100;
                mResult.PerFingerResult[resultIdx] = fingerSfbs;
                SetFingerResult(finger, fingerSfbs);

                resultIdx++;
            }

            mResult.PerHandResult[(int)eHand.Left] = tfs.SfbsPerHand[(int)eHand.Left] / totalBigramCount * 100;
            mResult.PerHandResult[(int)eHand.Right] = tfs.SfbsPerHand[(int)eHand.Right] / totalBigramCount * 100;

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

        public Sfss(AnalysisGraph graph) : base(eMeasurements.SameFingerSkipgrams, graph)
        {
            AddInputNode(eInputNodes.TwoFingerStats);
            mResult = new SFSResult(NodeId);
        }

        protected override void Compute()
        {
            var sameFingerStats = (TwoFingerStatsResult)AnalysisGraph.ResolvedNodes[eInputNodes.TwoFingerStats];
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
