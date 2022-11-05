using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Documents;
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
            AddInputNode(eInputNodes.TransformedCharacterToTrigramSet);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);

            int fingerCount = Utils.GetTokens<eFinger>().Count();
            mTgClassifications = new eTrigramClassification[fingerCount][][];

            for (int i = 0; i < mTgClassifications.Length; i++)
            {
                mTgClassifications[i] = new eTrigramClassification[fingerCount][];

                for (int j = 0; j < mTgClassifications[i].Length; j++)
                {
                    mTgClassifications[i][j] = new eTrigramClassification[fingerCount];
                }
            }

            for (int i = 0; i < fingerCount; i++)
            {
                for (int j = 0; j < fingerCount; j++)
                {
                    for (int k = 0; k < fingerCount; k++)
                    {
                        eHand firstHand = FingerUtil.GetHandForFingerAsInt(i);
                        eHand secondHand = FingerUtil.GetHandForFingerAsInt(j);
                        eHand thirdHand = FingerUtil.GetHandForFingerAsInt(k);

                        mTgClassifications[i][j][k] = ClassifyTrigram(i, j, k, firstHand, secondHand, thirdHand);
                    }
                }
            }
        }

        protected eTrigramClassification[][][] mTgClassifications;
        public override bool RespondsToGenerateSwapKeysEvent => true;

        // Helper array for fingers during swap keys to avoid allocation :( Gosh this will be hard to maintain.
        // Store the previous trigram classifications to make swap keys a snappy operation. Just reclassify the ones affected.
        eTrigramClassification[,,] mPreviousClassifications;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // Trying to do something to make alternations work, lets see what happens.
            byte c1 = mKb.TransformedKbState[k2i][k2j];
            byte c2 = mKb.TransformedKbState[k1i][k1j];

            // Subtract off the previous classification, and add on the new classification.

            var tgSet1 = mCharToTgSet.InvolvedTrigrams[c1];
            var tgSet2 = mCharToTgSet.InvolvedTrigrams[c2];

            for (int i = 0; i < tgSet1.Count; i++)
            {
                int previousFinger1 = 0;
                int previousFinger2 = 0;
                int previousFinger3 = 0;

                // If character2 is involved in this trigram, then the finger needs to be set to finger 1

                // All the fingers for each of the characters involved in the trigram have already been updated.
                // So we can get the new classification simply by using the trigram byte array.
                int finger1 = mC2f.CharacterToFinger[tgSet1[i].Item3[0]];
                int finger2 = mC2f.CharacterToFinger[tgSet1[i].Item3[1]];
                int finger3 = mC2f.CharacterToFinger[tgSet1[i].Item3[2]];

                eTrigramClassification newClassification = mTgClassifications[finger1][finger2][finger3];
            }

            for (int i = 0; i < tgSet2.Count; i++)
            {
            }
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private TransformedCharacterToFingerAsIntResult mC2f;
        private TransformedKbStateResult mKb;
        private TransformedCharacterToTrigramSetResult mCharToTgSet;
        private SortedTrigramSetResult mTgSet;

        protected enum eTrigramClassification : byte
        {
            Unclassified,
            Inroll,
            Outroll,
            Alternation,
            Redirect,
            BadRedirect,
            OneHand,
            SameFingerTrigram,
        }

        /// <summary>
        /// Matches the trigram with a pattern and returns its classification.
        /// </summary>
        /// <param name="firstFinger"></param>
        /// <param name="secondFinger"></param>
        /// <param name="thirdFinger"></param>
        /// <param name="firstHand"></param>
        /// <param name="secondHand"></param>
        /// <param name="thirdHand"></param>
        /// <returns></returns>
        protected eTrigramClassification ClassifyTrigram(int firstFinger, int secondFinger, int thirdFinger,
            eHand firstHand, eHand secondHand, eHand thirdHand)
        {
            if (firstFinger == (int)eFinger.None || 
                secondFinger == (int)eFinger.None ||
                thirdFinger == (int)eFinger.None)
            {
                return eTrigramClassification.Unclassified;
            }

            if (firstFinger == secondFinger && secondFinger == thirdFinger)
            {
                return eTrigramClassification.SameFingerTrigram;
            }

            // SFBS are not counted in trigram stats.
            if (firstFinger == secondFinger || secondFinger == thirdFinger)
            {
                return eTrigramClassification.Unclassified;
            }

            if (firstHand != secondHand && secondHand != thirdHand)
            {
                return eTrigramClassification.Alternation;
            }
            else if (firstHand == secondHand && secondHand == thirdHand)
            {
                // Could be either a one hand or a redirect
                if (firstFinger > secondFinger && secondFinger > thirdFinger ||
                    firstFinger < secondFinger && secondFinger < thirdFinger)
                {
                    return eTrigramClassification.OneHand;
                }

                return (firstFinger == (int)eFinger.LeftIndex || firstFinger == (int)eFinger.RightIndex ||
                        secondFinger == (int)eFinger.LeftIndex || secondFinger == (int)eFinger.RightIndex ||
                        thirdFinger == (int)eFinger.LeftIndex || thirdFinger == (int)eFinger.RightIndex)
                    ? eTrigramClassification.Redirect
                    : eTrigramClassification.BadRedirect;
            }
            else
            {
                // It has to be some form of roll.
                if (firstHand == secondHand)
                {
                    if (firstHand == eHand.Left)
                    {
                        if (firstFinger > secondFinger)
                        {
                            return eTrigramClassification.Outroll;
                        }
                        else
                        {
                            return eTrigramClassification.Inroll;
                        }
                    }
                    else
                    {
                        if (firstFinger > secondFinger)
                        {
                            return eTrigramClassification.Inroll;
                        }
                        else
                        {
                            return eTrigramClassification.Outroll;
                        }
                    }
                }
                else
                {
                    if (secondHand == eHand.Left)
                    {
                        if (secondFinger > thirdFinger)
                        {
                            return eTrigramClassification.Outroll;
                        }
                        else
                        {
                            return eTrigramClassification.Inroll;
                        }
                    }
                    else
                    {
                        if (secondFinger > thirdFinger)
                        {
                            return eTrigramClassification.Inroll;
                        }
                        else
                        {
                            return eTrigramClassification.Outroll;
                        }
                    }
                }
            }
        }

        protected override void Compute()
        {
            // Only allocate the previous classifications array if we have to.
            int expectedTgClassificationLen = SettingState.MeasurementSettings.CharFrequencyData.AvailableCharSet.Length;
            if (this.mPreviousClassifications == null ||
                this.mPreviousClassifications.Length != expectedTgClassificationLen)
            {
                this.mPreviousClassifications = new eTrigramClassification[expectedTgClassificationLen, expectedTgClassificationLen, expectedTgClassificationLen];
            }

            mC2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            var charToFinger = mC2f.CharacterToFinger;

            mKb = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var transformedKb = mKb.TransformedKbState;

            mTgSet = (SortedTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[
                eInputNodes.SortedTrigramSet];
            var trigrams = mTgSet.MostSignificantTrigrams;

            mCharToTgSet = (TransformedCharacterToTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[
                eInputNodes.TransformedCharacterToTrigramSet];

            // Index 0 is for the total, index 1 is for in rolls and index 2 is for out rolls.
            long totalInRolls = 0;
            long totalOutRolls = 0;
            long totalAlternations = 0;
            long totalRedirects = 0;
            long totalOneHands = 0;
            long oneHandsLeft = 0;
            long oneHandsRight = 0;
            long totalSameFingerTrigrams = 0;

            for (int i = 0; i < trigrams.Count; i++)
            {
                int firstFinger = charToFinger[trigrams[i].Item2[0]];
                int secondFinger = charToFinger[trigrams[i].Item2[1]];
                int thirdFinger = charToFinger[trigrams[i].Item2[2]];

                long freq = trigrams[i].Item1;

                eTrigramClassification tgClassification = mTgClassifications[firstFinger][secondFinger][thirdFinger];

                switch (tgClassification)
                {
                    case eTrigramClassification.Alternation:
                        totalAlternations += freq;
                        break;
                    case eTrigramClassification.Inroll:
                        totalInRolls += freq;
                        break;
                    case eTrigramClassification.Outroll:
                        totalOutRolls += freq;
                        break;
                    case eTrigramClassification.OneHand:
                        totalOneHands += freq;
                        break;
                    case eTrigramClassification.Redirect:
                        totalRedirects += freq;
                        break;
                    case eTrigramClassification.SameFingerTrigram:
                        totalSameFingerTrigrams += freq;
                        break;
                    case eTrigramClassification.Unclassified:
                        break;
                    case eTrigramClassification.BadRedirect:
                        // TODO
                        totalRedirects += freq;
                        break;
                }
            }

            mResult.TotalRolls = (totalInRolls + totalOutRolls);
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
