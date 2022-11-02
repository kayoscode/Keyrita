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
            AddInputNode(eInputNodes.SortedTrigramSet);
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

            mTg = (TransformedCharacterToTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToTrigramSet];
            var tgs = mTg.InvolvedTrigrams;

            mTgSet = (SortedTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.SortedTrigramSet];
            var allTgs = mTgSet.MostSignificantTrigrams;

            uint[,,] trigramFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq;

            // Index 0 is for the total, index 1 is for in rolls and index 2 is for out rolls.
            long totalInRolls = 0;
            long totalOutRolls = 0;
            long totalAlternations = 0;
            long totalRedirects = 0;
            long totalOneHands = 0;
            long oneHandsLeft = 0;
            long oneHandsRight = 0;

            for (int i = 0; i < allTgs.Count; i++)
            {
                // Classify the trigram, and modify the scores appropriately.
                var tg = allTgs[i];
                int firstFinger = charToFinger[tg.Item2[0]];
                int secondFinger = charToFinger[tg.Item2[1]];
                int thirdFinger = charToFinger[tg.Item2[2]];
                eHand firstHand = FingerUtil.GetHandForFingerAsInt(firstFinger);
                eHand secondHand = FingerUtil.GetHandForFingerAsInt(secondFinger);
                eHand thirdHand = FingerUtil.GetHandForFingerAsInt(thirdFinger);

                // Trigrams are not filtered, so there's a chance we could get a character which isn't even on the keyboard. If so, ignore that trigram.
                if(firstHand == eHand.None || secondHand == eHand.None || thirdHand == eHand.None ||
                    firstFinger == (int)eFinger.None || secondFinger == (int)eFinger.None || thirdFinger == (int)eFinger.None)
                {
                    continue;
                }

                eTrigramClassification tgc = ClassifyTrigram(firstFinger, secondFinger, thirdFinger, 
                    firstHand, secondHand, thirdHand);

                if(tgc == eTrigramClassification.Inroll)
                {
                    totalInRolls += tg.Item1;
                }
                else if(tgc == eTrigramClassification.Outroll)
                {
                    totalOutRolls += tg.Item1;
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
