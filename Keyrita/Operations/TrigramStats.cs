using System;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Operations
{
    public class TrigramStatsResult : AnalysisResult
    {
        public TrigramStatsResult(Enum resultId) : base(resultId)
        {
        }

        public double TotalRolls { get; set; }
        public double InRolls { get; set; }
        public double OutRolls { get; set; }
    }

    public class TrigramStats : OperationBase
    {
        protected TrigramStatsResult mResult;

        public TrigramStats()
            : base(eDependentOps.TrigramStats)
        {
            mResult = new TrigramStatsResult(this.Op);
            AddInputOp(eDependentOps.BigramClassification);
            AddInputOp(eDependentOps.TransfomedKbState);
            AddInputOp(eDependentOps.TransformedCharacterToFingerAsInt);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TransformedCharacterToFingerAsIntResult c2f = (TransformedCharacterToFingerAsIntResult)OperationSystem.ResolvedOps[eDependentOps.TransformedCharacterToFingerAsInt];
            var charToFinger = c2f.CharacterToFinger;

            TransformedKbStateResult transformedKbState = (TransformedKbStateResult)OperationSystem.ResolvedOps[eDependentOps.TransfomedKbState];
            var transformedKb = transformedKbState.TransformedKbState;

            BigramClassificationResult bgc = (BigramClassificationResult)OperationSystem.ResolvedOps[eDependentOps.BigramClassification];
            var bigramClassif = bgc.BigramClassifications;

            uint[,,] trigramFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq;
            long totalTrigrams = SettingState.MeasurementSettings.CharFrequencyData.TrigramHitCount;

            // Index 0 is for the total, index 1 is for in rolls and index 2 is for out rolls.
            long totalInRolls = 0;
            long totalOutRolls = 0;

            // Go through each bigram, and if it's classified as a roll, see if the first or last character use the other hand. If so,
            // we have a roll in the direction of the bigram classification.
            for(int i = 0; i < bigramClassif.GetLength(0); i++)
            {
                for(int j = 0; j < bigramClassif.GetLength(1); j++)
                {
                    if (bigramClassif[i, j] == BigramClassificationResult.eBigramClassification.InRoll ||
                        bigramClassif[i, j] == BigramClassificationResult.eBigramClassification.Outroll)
                    {
                        long totalRollsBg = 0;

                        eHand rollHand = FingerUtil.GetHandForFingerAsInt(charToFinger[i]);
                        eHand otherHand = FingerUtil.GetOtherHand(rollHand);
                        LTrace.Assert(rollHand == FingerUtil.GetHandForFingerAsInt(charToFinger[j]), "A roll must use the same hand.");

                        // Loop through every key on the keyboard, and if it's on the other hand, add both trigram stats. (before and after bigram)
                        for(int ki = 0; ki < transformedKb.Length; ki++)
                        {
                            for(int kj = 0; kj < transformedKb[ki].Length; kj++)
                            {
                                byte character = transformedKb[ki][kj];
                                var kf = charToFinger[character];
                                var kh = FingerUtil.GetHandForFingerAsInt(kf);

                                if(kh == otherHand)
                                {
                                    totalRollsBg += trigramFreq[i, j, character];
                                    totalRollsBg += trigramFreq[character, i, j];
                                }
                            }
                        }

                        if (bigramClassif[i, j] == BigramClassificationResult.eBigramClassification.InRoll)
                        {
                            totalInRolls += totalRollsBg;
                        }
                        else if (bigramClassif[i, j] == BigramClassificationResult.eBigramClassification.Outroll)
                        {
                            totalOutRolls += totalRollsBg;
                        }
                    }
                }
            }

            mResult.TotalRolls = ((totalInRolls + totalOutRolls) / (double)totalTrigrams) * 100;
            mResult.InRolls = totalInRolls / (double)totalTrigrams * 100;
            mResult.OutRolls = totalOutRolls / (double)totalTrigrams * 100;
        }
    }
}
