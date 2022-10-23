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

        public FindSFBs(Enum id) : base(id)
        {
            AddInputOp(eDependentOps.CharacterSetAsList);
            AddInputOp(eDependentOps.KeyToFingerAsInt);
            AddInputOp(eDependentOps.TransfomedKbState);
        }

        protected override void Compute()
        {
            result = new SFBResult(this.Op);

            CharacterSetAsListResult characterListResult = (CharacterSetAsListResult)OperationSystem.ResolvedOps[eDependentOps.CharacterSetAsList];
            var characterSet = characterListResult.CharacterSet;

            KeyToFingerAsIntResult k2fResult = (KeyToFingerAsIntResult)OperationSystem.ResolvedOps[eDependentOps.KeyToFingerAsInt];
            var keyToFingerInt = k2fResult.KeyToFinger;

            TransformedKbStateResult transformedKbState = (TransformedKbStateResult)OperationSystem.ResolvedOps[eDependentOps.TransfomedKbState];

            uint[,] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            long[] perFingerResult = new long[result.PerFingerResult.Count()];
            long[] perHandResult = new long[result.PerHandResult.Count()];
            long totalBigramCount = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            long sfbs = NativeAnalysis.MeasureTotalSFBs(transformedKbState.TransformedKbState, 
                bigramFreq, 
                keyToFingerInt, 
                perFingerResult);

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

        public override AnalysisResult GetResult()
        {
            return result;
        }
    }
}
