using System;
using System.Threading;
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
        public double OneHandsLeft { get; set; }
        public double OneHandsRight { get; set; }
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
            AddInputNode(eInputNodes.TransformedCharacterToTrigramSet);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // Trying to do something to make alternations work, lets see what happens.

        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private TransformedCharacterToFingerAsIntResult mC2f;
        private TransformedKbStateResult mKb;
        private TransformedCharacterToTrigramSetResult mTg;
        private SortedTrigramSetResult mTgSet;
        private BigramClassificationResult mBgc;

        protected enum eTrigramClassification
        {
            Inroll,
            Outroll,
            Alternation,
            Redirect,
            BadRedirect,
            OneHand,
            Unclassified
        }

        protected eTrigramClassification ClassifyTrigram(int firstFinger, int secondFinger, int thirdFinger,
            eHand firstHand, eHand secondHand, eHand thirdHand)
        {
            if (firstHand == secondHand)
            {
                // If the third hand is different, it's a roll.
                if(thirdHand != firstHand)
                {

                    if(firstHand == eHand.Left)
                    {
                        if(firstFinger < secondFinger)
                        {
                            return eTrigramClassification.Outroll;
                        }
                        else if(firstFinger > secondFinger)
                        {
                            return eTrigramClassification.Inroll;
                        }

                        return eTrigramClassification.Unclassified;
                    }
                    else
                    {
                        if(firstFinger < secondFinger)
                        {
                            return eTrigramClassification.Inroll;
                        }
                        else if(firstFinger > secondFinger)
                        {
                            return eTrigramClassification.Outroll;
                        }

                        return eTrigramClassification.Unclassified;
                    }
                }
                else
                {
                    // It could either be a one hand or a redirect.
                }
            }
            else
            {
                // It's a roll in this case.
                if(secondHand == thirdHand)
                {
                    if(secondHand == eHand.Left)
                    {
                        if(secondFinger < thirdFinger)
                        {
                            return eTrigramClassification.Outroll;
                        }
                        else if(secondFinger > thirdFinger)
                        {
                            return eTrigramClassification.Inroll;
                        }

                        return eTrigramClassification.Unclassified;
                    }
                    else
                    {
                        if(secondFinger < thirdFinger)
                        {
                            return eTrigramClassification.Inroll;
                        }
                        else if(secondFinger > thirdFinger)
                        {
                            return eTrigramClassification.Outroll;
                        }

                        return eTrigramClassification.Unclassified;
                    }
                }
                else
                {
                    // It could either be a redirect or one hand.
                }
            }

            return eTrigramClassification.Unclassified;
        }

        protected override void Compute()
        {
            mC2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            var charToFinger = mC2f.CharacterToFinger;

            mKb = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var transformedKb = mKb.TransformedKbState;

            mBgc = (BigramClassificationResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.BigramClassification];
            var bigramClassif = mBgc.BigramClassifications;

            uint[,,] trigramFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq;

            // Index 0 is for the total, index 1 is for in rolls and index 2 is for out rolls.
            long totalInRolls = 0;
            long totalOutRolls = 0;
            long totalAlternations = 0;
            long totalRedirects = 0;
            long totalOneHands = 0;
            long oneHandsLeft = 0;
            long oneHandsRight = 0;

            // Go through each bigram, and if it's classified as a roll, see if the first or last character use the other hand. If so,
            // we have a roll in the direction of the bigram classification.
            for (int i = 0; i < bigramClassif.GetLength(0); i++)
            {
                for (int j = 0; j < bigramClassif.GetLength(1); j++)
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
                        for (int ki = 0; ki < transformedKb.Length; ki++)
                        {
                            for (int kj = 0; kj < transformedKb[ki].Length; kj++)
                            {
                                byte character = transformedKb[ki][kj];
                                var kf = charToFinger[character];
                                var kh = FingerUtil.GetHandForFingerAsInt(kf);

                                if (kh == otherHand)
                                {
                                    totalRollsBg += trigramFreq[i, j, character];
                                    totalRollsBg += trigramFreq[character, i, j];
                                }
                                else
                                {

                                    // If its on the same hand, it's a redirect if the second bigram rolls the opposite direction as the first.
                                    var otherBgType = bigramClassif[j, character];
                                    if (otherBgType == BigramClassificationResult.eBigramClassification.InRoll ||
                                       otherBgType == BigramClassificationResult.eBigramClassification.Outroll)
                                    {
                                        if (otherBgType != bgType)
                                        {
                                            totalRedirects += trigramFreq[i, j, character];
                                        }
                                        else
                                        {
                                            var oneHands = trigramFreq[i, j, character];
                                            totalOneHands += oneHands;

                                            if (kh == eHand.Left)
                                            {
                                                oneHandsLeft += oneHands;
                                            }
                                            else
                                            {
                                                oneHandsRight += oneHands;
                                            }
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

            mResult.TotalRolls = totalInRolls + totalOutRolls;
            mResult.InRolls = totalInRolls;
            mResult.OutRolls = totalOutRolls;
            mResult.TotalAlternations = totalAlternations;
            mResult.TotalRedirects = totalRedirects;
            mResult.TotalOneHands = totalOneHands;
            mResult.OneHandsLeft = oneHandsLeft;
            mResult.OneHandsRight = oneHandsRight;
        }
    }
}
