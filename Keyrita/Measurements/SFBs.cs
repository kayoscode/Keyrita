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
        protected SFBResult result;

        public FindSFBs() : base(eMeasurements.SameFingerBigram)
        {
            AddInputOp(eDependentOps.BigramClassification);
            AddInputOp(eDependentOps.TransformedCharacterToFingerAsInt);
        }

        protected override void Compute()
        {
            result = new SFBResult(this.Op);

            BigramClassificationResult bgc = (BigramClassificationResult)OperationSystem.ResolvedOps[eDependentOps.BigramClassification];
            TransformedCharacterToFingerAsIntResult c2f = (TransformedCharacterToFingerAsIntResult)OperationSystem.ResolvedOps[eDependentOps.TransformedCharacterToFingerAsInt];

            uint[,] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            long[] perFingerResult = new long[result.PerFingerResult.Count()];
            long[] perHandResult = new long[result.PerHandResult.Count()];
            long totalBigramCount = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            long sfbs = CalculateTotalSFBs(bgc.BigramClassifications, bigramFreq, perFingerResult, c2f.CharacterToFinger);

            double totalSfbs = (double)sfbs / totalBigramCount;
            result.TotalBigrams = totalSfbs * 100;

            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                double fingerSfbs = ((double)perFingerResult[resultIdx] / totalBigramCount) * 100;
                result.PerFingerResult[resultIdx] = fingerSfbs;
                SetFingerResult(finger, fingerSfbs);

                resultIdx++;
            }

            SetTotalResult(result.TotalBigrams);
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
                        LTrace.Assert(i != j, "An SFB should never be the same key");

                        totalSfbs += bigramFreq[i, j];
                        // i and j have the same finger.
                        LTrace.Assert(characterToFinger[i] == characterToFinger[j], "An SFB should always use the same fingers for both keys");
                        perFingerResults[characterToFinger[i]] += bigramFreq[i, j];
                    }
                }
            }

            return totalSfbs;
        }

        public override AnalysisResult GetResult()
        {
            return result;
        }
    }
}
