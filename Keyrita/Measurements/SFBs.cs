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

        public double TotalBigrams { get; set; }
        public double[] PerHandResult { get; private set; } = new double[2];
        public double[] PerFingerResult { get; private set; } = new double[Utils.GetTokens<eFinger>().Count()];
    }

    public class FindSFBs : FingerHandMeasurement
    {
        protected SFBResult mResult;

        public FindSFBs() : base(eMeasurements.SameFingerBigram)
        {
            AddInputNode(eInputNodes.BigramClassification);
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);
        }

        protected override void Compute()
        {
            mResult = new SFBResult(this.NodeId);

            BigramClassificationResult bgc = (BigramClassificationResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.BigramClassification];
            TransformedCharacterToFingerAsIntResult c2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];

            uint[,] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            long[] perFingerResult = new long[mResult.PerFingerResult.Count()];
            long[] perHandResult = new long[mResult.PerHandResult.Count()];
            long totalBigramCount = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            long sfbs = CalculateTotalSFBs(bgc.BigramClassifications, bigramFreq, perFingerResult, c2f.CharacterToFinger);

            double totalSfbs = (double)sfbs / totalBigramCount;
            mResult.TotalBigrams = totalSfbs * 100;

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerSfbs = ((double)perFingerResult[resultIdx] / totalBigramCount) * 100;
                mResult.PerFingerResult[resultIdx] = fingerSfbs;
                SetFingerResult(finger, fingerSfbs);

                resultIdx++;
            }

            SetTotalResult(mResult.TotalBigrams);
        }

        protected long CalculateTotalSFBs(BigramClassificationResult.eBigramClassification[,] bigramClassifications, 
            uint[,] bigramFreq, 
            long[] perFingerResults,
            int[] characterToFinger)
        {
            long totalSfbs = 0;

            for(int i = 0; i < bigramClassifications.GetLength(0); i++)
            {
                for(int j = 0; j < bigramClassifications.GetLength(1); j++)
                {
                    if (bigramClassifications[i, j] == BigramClassificationResult.eBigramClassification.SFB)
                    {
                        LogUtils.Assert(i != j, "An SFB should never be the same key");

                        totalSfbs += bigramFreq[i, j];
                        // i and j have the same finger.
                        LogUtils.Assert(characterToFinger[i] == characterToFinger[j], "An SFB should always use the same fingers for both keys");
                        perFingerResults[characterToFinger[i]] += bigramFreq[i, j];
                    }
                }
            }

            return totalSfbs;
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }
}
