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
    public interface ISFBResult
    {
        double TotalBigrams { get; }
    }

    public class SFBResult : AnalysisResult, ISFBResult
    {
        public SFBResult(Enum resultId) 
            : base(resultId)
        {
        }

        public double TotalBigrams { get; set; }
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

            long sfbs = NativeAnalysis.MeasureTotalSFBs(transformedKbState.TransformedKbState, bigramFreq, keyToFingerInt);
            double totalSfbs = (double)sfbs / (double)SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            result.TotalBigrams = totalSfbs * 100;

            SetTotalResult(result.TotalBigrams);
        }

        public override AnalysisResult GetResult()
        {
            return result;
        }
    }
}
