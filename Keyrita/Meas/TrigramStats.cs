using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Operations
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

        public TrigramStats()
            : base(eInputNodes.TrigramStats)
        {
            mResult = new TrigramStatsResult(this.NodeId);
            mPreviousResult = new TrigramStatsResult(this.NodeId);

            AddInputNode(eInputNodes.TransformedCharacterToTrigramSet);
            AddInputNode(eInputNodes.SortedTrigramSet);
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
        protected eTrigramClassification[][][] mPreviousClasifications;
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
                    // TODO
                    mResult.TotalRedirects -= freq;
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
                    // TODO
                    mResult.TotalRedirects += freq;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReclassifyTrigrams(List<byte[]> trigramSet, int c1, int c2, int caller)
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

                var expectedPreviousClassification = mPreviousClasifications[trigramSet[i][0]][trigramSet[i][1]][trigramSet[i][2]];
                if (previousClassification != expectedPreviousClassification)
                {
                    LogUtils.LogInfo("Incorrect previous classification");
                }

                mPreviousClasifications[trigramSet[i][0]][trigramSet[i][1]][trigramSet[i][2]] = newClassification;
                LogUtils.LogInfo($"updasting class class: {caller}: {trigramSet[i][0]} {trigramSet[i][1]} {trigramSet[i][2]}: Used fingers: {newFinger1}{newFinger2}{newFinger3} -> {newClassification}");

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
            mPreviousResult.TotalAlternations = mResult.TotalAlternations;
            mPreviousResult.TotalOneHands = mResult.TotalOneHands;

            // Trying to do something to make alternations work, lets see what happens.
            byte c1 = mKb.TransformedKbState[k2i][k2j];
            byte c2 = mKb.TransformedKbState[k1i][k1j];

            LogUtils.Assert(c1 != c2);

            // Subtract off the previous classification, and add on the new classification.
            var tgSet1 = mCharToTgSet.FirstInvolvedTrigrams[c1][c2];
            var tgSet2 = mCharToTgSet.FirstInvolvedTrigrams[c2][c1];

            // Update stats for the trigrams that contain both letters.
            var bothLettersTgs1 = mCharToTgSet.BothInvolvedTrigrams[c1][c2];

            ReclassifyTrigrams(tgSet1, c1, c2, 0);
            ReclassifyTrigrams(tgSet2, c1, c2, 1);
            ReclassifyTrigrams(bothLettersTgs1, c1, c2, 2);

            mResult.TotalRolls = mResult.InRolls + mResult.OutRolls;
        }

        public override void SwapBack()
        {
            mResult.TotalRolls = mPreviousResult.TotalRolls;
            mResult.InRolls = mPreviousResult.InRolls;
            mResult.OutRolls = mPreviousResult.OutRolls;
            mResult.TotalRedirects = mPreviousResult.TotalRedirects;
            mResult.TotalAlternations = mPreviousResult.TotalAlternations;
            mResult.TotalOneHands = mPreviousResult.TotalOneHands;
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private TransformedCharacterToFingerAsIntResult mC2f;
        private TransformedCharacterToTrigramSetResult mCharToTgSet;
        private SortedTrigramSetResult mTgSet;
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
            int numTrigrams = SettingState.MeasurementSettings.CharFrequencyData.AvailableCharSet.Length;
            mPreviousClasifications = new eTrigramClassification[numTrigrams][][];

            for (int i = 0; i < mPreviousClasifications.Length; i++)
            {
                mPreviousClasifications[i] = new eTrigramClassification[numTrigrams][];

                for (int j = 0; j < mPreviousClasifications[i].Length; j++)
                {
                    mPreviousClasifications[i][j] = new eTrigramClassification[numTrigrams];
                }
            }

            mC2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            mTgSet = (SortedTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[
                eInputNodes.SortedTrigramSet];
            mKb = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[
                eInputNodes.TransfomedKbState];
            mCharToTgSet = (TransformedCharacterToTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[
                eInputNodes.TransformedCharacterToTrigramSet];

            ComputeResults();
        }

        protected void ComputeResults()
        {
            var charToFinger = mC2f.CharacterToFinger;
            var trigrams = mTgSet.MostSignificantTrigrams;

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
                int firstFinger = charToFinger[trigrams[i].Chars[0]];
                int secondFinger = charToFinger[trigrams[i].Chars[1]];
                int thirdFinger = charToFinger[trigrams[i].Chars[2]];

                long freq = trigrams[i].Frequency;

                eTrigramClassification tgClassification = mTgClassifications[firstFinger][secondFinger][thirdFinger];
                mPreviousClasifications[trigrams[i].Chars[0]][trigrams[i].Chars[1]][trigrams[i].Chars[2]] = tgClassification;

                LogUtils.LogInfo($"Creating class: {trigrams[i].Chars[0]} {trigrams[i].Chars[1]} {trigrams[i].Chars[2]}: Used fingers: {firstFinger}{secondFinger}{thirdFinger} -> {tgClassification}");

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
