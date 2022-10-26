using System;
using System.Windows.Documents.Serialization;
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

        public double TotalAlternations { get; set; }

        public double TotalRedirects { get; set; }
        public double TotalOneHands { get; set; }
    }

    /// <summary>
    /// Stats computed on each trigram.
    /// </summary>
    public class TrigramStats : GraphNode
    {
        protected TrigramStatsResult mResult;

        public TrigramStats()
            : base(eInputNodes.TrigramStats)
        {
            mResult = new TrigramStatsResult(this.NodeId);
            AddInputNode(eInputNodes.BigramClassification);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TransformedCharacterToFingerAsIntResult c2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            var charToFinger = c2f.CharacterToFinger;

            TransformedKbStateResult transformedKbState = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var transformedKb = transformedKbState.TransformedKbState;

            BigramClassificationResult bgc = (BigramClassificationResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.BigramClassification];
            var bigramClassif = bgc.BigramClassifications;

            uint[,,] trigramFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq;
            long totalTrigrams = SettingState.MeasurementSettings.CharFrequencyData.TrigramHitCount;

            // Index 0 is for the total, index 1 is for in rolls and index 2 is for out rolls.
            long totalInRolls = 0;
            long totalOutRolls = 0;
            long totalAlternations = 0;
            long totalRedirects = 0;
            long totalOneHands = 0;

            // Go through each bigram, and if it's classified as a roll, see if the first or last character use the other hand. If so,
            // we have a roll in the direction of the bigram classification.
            for(int i = 0; i < bigramClassif.GetLength(0); i++)
            {
                for(int j = 0; j < bigramClassif.GetLength(1); j++)
                {
                    BigramClassificationResult.eBigramClassification bgType = bigramClassif[i, j];

                    if (bgType == BigramClassificationResult.eBigramClassification.InRoll ||
                        bgType == BigramClassificationResult.eBigramClassification.Outroll)
                    {
                        long totalRollsBg = 0;

                        eHand rollHand = FingerUtil.GetHandForFingerAsInt(charToFinger[i]);
                        eHand otherHand = FingerUtil.GetOtherHand(rollHand);
                        LogUtils.Assert(rollHand == FingerUtil.GetHandForFingerAsInt(charToFinger[j]), "A roll must use the same hand.");

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
                                else
                                {
                                    
                                    // If its on the same hand, it's a redirect if the second bigram rolls the opposite direction as the first.
                                    var otherBgType = bigramClassif[j, character];
                                    if(otherBgType == BigramClassificationResult.eBigramClassification.InRoll ||
                                       otherBgType == BigramClassificationResult.eBigramClassification.Outroll)
                                    {
                                        if(otherBgType != bgType)
                                        {
                                            totalRedirects += trigramFreq[i, j, character];
                                        }
                                        else
                                        {
                                            totalOneHands += trigramFreq[i, j, character];
                                        }
                                    }
                                    else
                                    {
                                        // Here it should either be the same character or an sfb.
                                        LogUtils.Assert(j == character ||
                                            bigramClassif[j, character] == BigramClassificationResult.eBigramClassification.SFB);
                                    }
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
                    // For alternations, we just classify them using h1 h2 h1. Rolls handle h1 h1 h2 and h1 h2 h2.
                    else if (bigramClassif[i, j] == BigramClassificationResult.eBigramClassification.Alternation)
                    {
                        // Use all keys starting with the same hand as the first character.
                        eHand startHand = FingerUtil.GetHandForFingerAsInt(charToFinger[i]);

                        // Loop through every key on the keyboard, and if it's on the other hand, add both trigram stats. (before and after bigram)
                        for (int ki = 0; ki < transformedKb.Length; ki++)
                        {
                            for (int kj = 0; kj < transformedKb[ki].Length; kj++)
                            {
                                byte character = transformedKb[ki][kj];
                                var kf = charToFinger[character];
                                var kh = FingerUtil.GetHandForFingerAsInt(kf);

                                if (kh == startHand)
                                {
                                    totalAlternations += trigramFreq[i, j, character];
                                }
                            }
                        }
                    }
                }
            }

            mResult.TotalRolls = ((totalInRolls + totalOutRolls) / (double)totalTrigrams) * 100;
            mResult.InRolls = totalInRolls / (double)totalTrigrams * 100;
            mResult.OutRolls = totalOutRolls / (double)totalTrigrams * 100;
            mResult.TotalAlternations = totalAlternations / (double)totalTrigrams * 100;
            mResult.TotalRedirects = totalRedirects / (double)totalTrigrams * 100;
            mResult.TotalOneHands = totalOneHands / (double)totalTrigrams * 100;
        }
    }
}
