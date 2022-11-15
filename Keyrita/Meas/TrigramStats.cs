using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using Keyrita.Analysis.AnalysisUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Analysis
{
    /// <summary>
    /// All the valid trigram classification values.
    /// </summary>
    public enum eTrigramClassification : byte
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

    public class TrigramStatsResult : AnalysisResult
    {
        public TrigramStatsResult(Enum resultId) : base(resultId)
        {
        }

        public long TotalRolls { get; set; }
        public long InRolls { get; set; }
        public long OutRolls { get; set; }

        public long TotalAlternations { get; set; }

        public long TotalRedirects { get; set; }
        public long TotalBadRedirects { get; set; }

        public long TotalOneHands { get; set; }
        public long OneHandsLeft { get; set; }
        public long OneHandsRight { get; set; }
    }

    /// <summary>
    /// Stats computed on each trigram.
    /// </summary>
    public class TrigramStats : GraphNode
    {
        protected TrigramStatsResult mResult;
        protected TrigramStatsResult mPreviousResult;

        public TrigramStats(AnalysisGraph graph)
            : base(eInputNodes.TrigramStats, graph)
        {
            mResult = new TrigramStatsResult(this.NodeId);
            mPreviousResult = new TrigramStatsResult(this.NodeId);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AdjustToNewTrigramClassification(eTrigramClassification oldClassification, eTrigramClassification classification, long freq)
        {
            switch (oldClassification)
            {
                case eTrigramClassification.Alternation:
                    mResult.TotalAlternations -= freq;
                    break;
                case eTrigramClassification.Inroll:
                    mResult.InRolls -= freq;
                    break;
                case eTrigramClassification.Outroll:
                    mResult.OutRolls -= freq;
                    break;
                case eTrigramClassification.OneHand:
                    mResult.TotalOneHands -= freq;
                    break;
                case eTrigramClassification.Redirect:
                    mResult.TotalRedirects -= freq;
                    break;
                case eTrigramClassification.SameFingerTrigram:
                    // TODO
                    break;
                case eTrigramClassification.Unclassified:
                    break;
                case eTrigramClassification.BadRedirect:
                    mResult.TotalBadRedirects -= freq;
                    break;
            }

            switch (classification)
            {
                case eTrigramClassification.Alternation:
                    mResult.TotalAlternations += freq;
                    break;
                case eTrigramClassification.Inroll:
                    mResult.InRolls += freq;
                    break;
                case eTrigramClassification.Outroll:
                    mResult.OutRolls += freq;
                    break;
                case eTrigramClassification.OneHand:
                    mResult.TotalOneHands += freq;
                    break;
                case eTrigramClassification.Redirect:
                    mResult.TotalRedirects += freq;
                    break;
                case eTrigramClassification.SameFingerTrigram:
                    // TODO
                    break;
                case eTrigramClassification.Unclassified:
                    break;
                case eTrigramClassification.BadRedirect:
                    mResult.TotalBadRedirects += freq;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReclassifyTrigrams(List<byte[]> trigramSet, int c1, int c2)
        {
            for (int i = 0; i < trigramSet.Count; i++)
            {
                int newFinger1 = mC2f.CharacterToFinger[trigramSet[i][0]];
                int newFinger2 = mC2f.CharacterToFinger[trigramSet[i][1]];
                int newFinger3 = mC2f.CharacterToFinger[trigramSet[i][2]];

                int previousFinger1 = newFinger1;
                int previousFinger2 = newFinger2;
                int previousFinger3 = newFinger3;

                if(c1 == trigramSet[i][0])
                {
                    previousFinger1 = mC2f.CharacterToFinger[c2];
                }
                else if(c2 == trigramSet[i][0])
                {
                    previousFinger1 = mC2f.CharacterToFinger[c1];
                }
                
                if(c1 == trigramSet[i][1])
                {
                    previousFinger2 = mC2f.CharacterToFinger[c2];
                }
                else if(c2 == trigramSet[i][1])
                {
                    previousFinger2 = mC2f.CharacterToFinger[c1];
                }

                if(c1 == trigramSet[i][2])
                {
                    previousFinger3 = mC2f.CharacterToFinger[c2];
                }
                else if(c2 == trigramSet[i][2])
                {
                    previousFinger3 = mC2f.CharacterToFinger[c1];
                }

                eTrigramClassification previousClassification = mTgClassifications[previousFinger1][previousFinger2][previousFinger3];
                eTrigramClassification newClassification = mTgClassifications[newFinger1][newFinger2][newFinger3];

                AdjustToNewTrigramClassification(previousClassification, newClassification,
                    SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[trigramSet[i][0]][trigramSet[i][1]][trigramSet[i][2]]);
            }
        }

        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            mPreviousResult.TotalRolls = mResult.TotalRolls;
            mPreviousResult.InRolls = mResult.InRolls;
            mPreviousResult.OutRolls = mResult.OutRolls;

            mPreviousResult.TotalRedirects = mResult.TotalRedirects;
            mPreviousResult.TotalBadRedirects = mResult.TotalBadRedirects;
            mPreviousResult.TotalAlternations = mResult.TotalAlternations;
            mPreviousResult.TotalOneHands = mResult.TotalOneHands;

            // Trying to do something to make alternations work, lets see what happens.
            byte c1 = mKb.TransformedKbState[k1i][k1j];
            byte c2 = mKb.TransformedKbState[k2i][k2j];

            LogUtils.Assert(c1 != c2);

            // Subtract off the previous classification, and add on the new classification.
            var tgSet1 = SettingState.MeasurementSettings.CharacterToTrigramSet.FirstInvolvedTrigrams[c1][c2];
            var tgSet2 = SettingState.MeasurementSettings.CharacterToTrigramSet.FirstInvolvedTrigrams[c2][c1];

            // Update stats for the trigrams that contain both letters.
            var bothLettersTgs1 = SettingState.MeasurementSettings.CharacterToTrigramSet.BothInvolvedTrigrams[c1][c2];

            ReclassifyTrigrams(tgSet1, c1, c2);
            ReclassifyTrigrams(tgSet2, c1, c2);
            ReclassifyTrigrams(bothLettersTgs1, c1, c2);

            mResult.TotalRolls = mResult.InRolls + mResult.OutRolls;
        }

        public override void SwapBack()
        {
            mResult.TotalRolls = mPreviousResult.TotalRolls;
            mResult.InRolls = mPreviousResult.InRolls;
            mResult.OutRolls = mPreviousResult.OutRolls;

            mResult.TotalRedirects = mPreviousResult.TotalRedirects;
            mResult.TotalBadRedirects = mPreviousResult.TotalBadRedirects;
            mResult.TotalAlternations = mPreviousResult.TotalAlternations;
            mResult.TotalOneHands = mPreviousResult.TotalOneHands;
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private TransformedCharacterToFingerAsIntResult mC2f;
        private TransformedKbStateResult mKb;

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
            mC2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraph.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            mKb = (TransformedKbStateResult)AnalysisGraph.ResolvedNodes[
                eInputNodes.TransfomedKbState];

            ComputeResults();
        }

        protected void ComputeResults()
        {
            var charToFinger = mC2f.CharacterToFinger;
            var trigrams = SettingState.MeasurementSettings.SortedTrigramSet.SortedTrigramSet;

            // Index 0 is for the total, index 1 is for in rolls and index 2 is for out rolls.
            long totalInRolls = 0;
            long totalOutRolls = 0;
            long totalAlternations = 0;
            long totalRedirects = 0;
            long totalBadRedirects = 0;
            long totalOneHands = 0;
            long oneHandsLeft = 0;
            long oneHandsRight = 0;
            long totalSameFingerTrigrams = 0;

            for (int i = 0; i < trigrams.Count; i++)
            {
                int firstFinger = charToFinger[trigrams[i][0]];
                int secondFinger = charToFinger[trigrams[i][1]];
                int thirdFinger = charToFinger[trigrams[i][2]];

                long freq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[trigrams[i][0]][trigrams[i][1]][trigrams[i][2]];

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
                        totalBadRedirects += freq;
                        break;
                }
            }

            mResult.TotalRolls = (totalInRolls + totalOutRolls);
            mResult.InRolls = totalInRolls;
            mResult.OutRolls = totalOutRolls;
            mResult.TotalAlternations = totalAlternations;

            mResult.TotalRedirects = totalRedirects;
            mResult.TotalBadRedirects = totalBadRedirects;
            mResult.TotalOneHands = totalOneHands;
            mResult.OneHandsLeft = oneHandsLeft;
            mResult.OneHandsRight = oneHandsRight;
        }
    }
}
