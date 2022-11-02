using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Keyrita.Measurements;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Operations
{
    public enum eInputNodes
    {
        CharacterSetAsList,
        TransformedCharacterToKey,
        KeyToFingerAsInt,
        TransformedCharacterToFingerAsInt,
        TransfomedKbState,
        BigramClassification,
        TrigramStats,
        TwoFingerStats,
        FingerAsIntToHomePosition,
        KeyLag,
        SameFingerMap,
        ScissorsIntermediate,
        TransformedCharacterToTrigramSet,
        SortedTrigramSet
    }

    public class SortedTrigramSetResult : AnalysisResult
    {
        public SortedTrigramSetResult(Enum resultId) : base(resultId)
        {
        }

        public List<(long, byte[])> MostSignificantTrigrams { get; set; } = new List<(long, byte[])>();
        public long TrigramCoverage { get; set; } = 0;
    }

    public class SortedTrigramSet : GraphNode
    {
        private SortedTrigramSetResult mResult;
        public SortedTrigramSet() : base(eInputNodes.SortedTrigramSet)
        {
            mResult = new SortedTrigramSetResult(this.NodeId);
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            int tgCount = SettingState.MeasurementSettings.TrigramDepth.Value;

            LogUtils.Assert(SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.GetLength(0) <= byte.MaxValue);
            mResult.MostSignificantTrigrams.Clear();

            // Go through every trigram and setup append to each list the correct values.
            for(int i = 0; i < SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.GetLength(0); i++)
            {
                for (int j = 0; j < SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.GetLength(1); j++)
                {
                    for (int k = 0; k < SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.GetLength(2); k++)
                    {
                        byte[] trigram = new byte[3];
                        trigram[0] = (byte)i;
                        trigram[1] = (byte)j;
                        trigram[2] = (byte)k;

                        long trigramFreq = SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq[i, j, k];

                        mResult.MostSignificantTrigrams.Add((trigramFreq, trigram));
                    }
                }
            }

            // Sort them, then remove the unnecessary ones.
            mResult.MostSignificantTrigrams.Sort(new Comparison<(long, byte[])>((a, b) =>
            {
                if (a.Item1 == b.Item1) return 0;
                return a.Item1 > b.Item1 ? -1 : 1;
            }));

            // Remove the unnecessary trigrams.
            mResult.MostSignificantTrigrams.RemoveRange(tgCount, mResult.MostSignificantTrigrams.Count() - tgCount);

            long tgCoverage = 0;
            for(int i = 0; i < mResult.MostSignificantTrigrams.Count(); i++)
            {
                tgCoverage += mResult.MostSignificantTrigrams[i].Item1;
            }

            mResult.TrigramCoverage = tgCoverage;
        }
    }

    public class TransformedCharacterToTrigramSetResult : AnalysisResult
    {
        public TransformedCharacterToTrigramSetResult(Enum resultId) : base(resultId)
        {
        }

        /// <summary>
        /// Stores a tuple with the following information.
        /// 1. The index in the byte array this character resides.
        /// 2. The trigramFrequency.
        /// 3. The three characters representing the trigram. Do not mutate.
        /// </summary>
        public List<(int, long, byte[])>[] InvolvedTrigrams { get; set; }
    }

    /// <summary>
    /// Creates an array mapping a transformed character to a list of trigrams its involved in.
    /// </summary>
    public class TransformedCharacterToTrigramSet : GraphNode
    {
        private TransformedCharacterToTrigramSetResult mResult;

        public TransformedCharacterToTrigramSet() : base(eInputNodes.TransformedCharacterToTrigramSet)
        {
            mResult = new TransformedCharacterToTrigramSetResult(this.NodeId);
            AddInputNode(eInputNodes.SortedTrigramSet);
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            mResult = new TransformedCharacterToTrigramSetResult(this.NodeId);
            mResult.InvolvedTrigrams = new List<(int, long, byte[])>[SettingState.MeasurementSettings.CharFrequencyData.TrigramFreq.GetLength(0)];

            var allTrigrams = (SortedTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.SortedTrigramSet];

            for(int i = 0; i < mResult.InvolvedTrigrams.Count(); i++)
            {
                mResult.InvolvedTrigrams[i] = new();
            }

            for(int i = 0; i < allTrigrams.MostSignificantTrigrams.Count; i++)
            {
                for(int j = 0; j < allTrigrams.MostSignificantTrigrams[i].Item2.Length; j++)
                {
                    mResult.InvolvedTrigrams[allTrigrams.MostSignificantTrigrams[i].Item2[j]].Add((j,
                        allTrigrams.MostSignificantTrigrams[i].Item1,
                        allTrigrams.MostSignificantTrigrams[i].Item2));
                }
            }
        }
    }

    /// <summary>
    /// Given a key on the keyboard, find a weighted sum of its same finger bigrams and 
    /// </summary>
    public class KeyLagResult : PerKeyAnalysisResult
    {
        public KeyLagResult(Enum resultId) : base(resultId)
        {
        }

        public double TotalResult { get; set; }
    }

    public class KeyLag : GraphNode
    {
        // These all need to eventually be settings.
        private const double SFB_WEIGHT = 1000;
        private const double SFS_WEIGHT = 500;
        private const double SCISSOR_WEIGHT = 1700;

        // Penalty applied to key scores just for existing at their location :D
        // Eventually let the user set these and discriminate their own way!
        private static double[,] KEY_LOCATION_PENALTY = new double[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS]
        {
            { 1.85, 1.35, 1.2, 1.1, 1.05, 1.05, 1.1, 1.2, 1.35, 1.85 },
            { 1.4, 1.2, 1.05, 1.0, 1.2, 1.2, 1.0, 1.05, 1.2, 1.5 },
            { 1.85, 1.55, 1.25, 1.1, 1.3, 1.3, 1.1, 1.25, 1.55, 1.85 },
        };

        private KeyLagResult mResult;
        public KeyLag() :
            base(eInputNodes.KeyLag)
        {
            AddInputNode(eInputNodes.TwoFingerStats);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
            AddInputNode(eInputNodes.ScissorsIntermediate);
            mResult = new KeyLagResult(NodeId);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private KeyToFingerAsIntResult mK2f;
        private TwoFingerStatsResult mTfs;
        private ScissorsResult mSr;

        protected override void Compute()
        {
            mK2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            mTfs = (TwoFingerStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TwoFingerStats];
            mSr = (ScissorsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.ScissorsIntermediate];
            ComputeResult();
        }

        private void ComputeResult()
        {
            var scissorValues = mSr.PerKeyResult;
            var sfbsPerKey = mTfs.SfbDistancePerKey;
            var sfsPerKey = mTfs.SfsDistancePerKey;
            var keyToFinger = mK2f.KeyToFinger;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            mResult.TotalResult = 0;

            // This intermediate measurement is mainly used for evaluation/generate, but
            // is also used for the finger speed meaurement. 
            // Each key is assigned a weighted sum of the same finger grams coming from it weighted differently depending on the depth.
            for(int i = 0; i < sfbsPerKey.GetLength(0); i++)
            {
                for(int j = 0; j < sfbsPerKey.GetLength(1); j++)
                {
                    var finger = keyToFinger[i][j];
                    var fingerWeight = SettingState.FingerSettings.FingerWeights.GetValueAt(finger);

                    double score = sfbsPerKey[i, j] * SFB_WEIGHT;
                    score += sfsPerKey[i, j] * SFS_WEIGHT;
                    score += (scissorValues[i, j] / totalBg) * SCISSOR_WEIGHT;

                    score *= fingerWeight;
                    score *= KEY_LOCATION_PENALTY[i, j]; // Sucks to suck.
                    mResult.PerKeyResult[i, j] = score;
                    mResult.TotalResult += score;
                }
            }
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // So much would have changed by the time we get here, just recompute it all.
            ComputeResult();
        }
    }

    /// <summary>
    /// Returns information relating to what happens when you interact with all the bigrams.
    /// </summary>
    public class TwoFingerStatsResult : AnalysisResult
    {
        public TwoFingerStatsResult(Enum resultId) 
            : base(resultId)
        {
        }

        public long TotalSfbs { get; set; }
        public long[] SfbsPerHand { get; private set; } = new long[Utils.GetTokens<eHand>().Count()];
        public long[] SfbsPerFinger { get; private set; } = new long[Utils.GetTokens<eFinger>().Count()];
        public double[,] SfbDistancePerKey { get; private set; } = new double[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];

        public long TotalSfs { get; set; }
        public long[] SfsPerHand { get; private set; } = new long[Utils.GetTokens<eHand>().Count()];
        public long[] SfsPerFinger { get; private set; } = new long[Utils.GetTokens<eFinger>().Count()];
        public double[,] SfsDistancePerKey { get; private set; } = new double[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
    }

    /// <summary>
    /// Computes frequencies related to which keys are on which same fingers.
    /// These results will be used downstream for various measurements. The results may be directly output in some measurements.
    /// </summary>
    public class TwoFingerStats : GraphNode
    {
        protected TwoFingerStatsResult mResult;

        public TwoFingerStats() : base(eInputNodes.TwoFingerStats)
        {
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.SameFingerMap);
        }

        private TransformedCharacterToFingerAsIntResult mC2f;
        private TransformedKbStateResult mKb;
        private SameFingerMapResult mSfm;

        protected override void Compute()
        {
            mResult = new TwoFingerStatsResult(NodeId);

            mC2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            mKb = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            mSfm = (SameFingerMapResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.SameFingerMap];

            uint[,] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            uint[,] skipgram2Freq = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2Freq;
            double totalSkipgram2 = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2HitCount;

            var characterToFinger = mC2f.CharacterToFinger;
            var sameFingerMap = mSfm.SameFingerKeysPerKey;

            // Loop over all the keys, and get its same finger map.
            for(int i = 0; i < sameFingerMap.GetLength(0); i++)
            {
                for(int j = 0; j < sameFingerMap.GetLength(1); j++)
                {
                    var thisKeySfbMap = sameFingerMap[i, j];

                    // Get every sfb for that key.
                    for(int k = 0; k < thisKeySfbMap.Count; k++)
                    {
                        var secondCharPos = thisKeySfbMap[k];
                        var ch1 = mKb.TransformedKbState[i][j];
                        var ch2 = mKb.TransformedKbState[secondCharPos.Item1][secondCharPos.Item2];

                        var distance = FingerUtil.MovementDistance[i, j, secondCharPos.Item1, secondCharPos.Item2];
                        LogUtils.Assert(!(i == secondCharPos.Item1 && j == secondCharPos.Item2), "An SFB should never be the same key");

                        // Add same finger stats for the bigram. Because we know that each bigram which is classified as an sfb will be hit with the same finger, it doesn't
                        // matter if we are searching for sfbs, sfs, or skipgrams(n). Just make sure to use the correct frequency data.
                        mResult.TotalSfbs += bigramFreq[ch1, ch2];
                        mResult.SfbDistancePerKey[i, j] += (bigramFreq[ch1, ch2] / totalBg) * distance;
                        mResult.TotalSfs += skipgram2Freq[ch1, ch2];
                        mResult.SfsDistancePerKey[i, j] += (skipgram2Freq[ch1, ch2] / totalSkipgram2) * distance;

                        // i and j have the same finger.
                        LogUtils.Assert(characterToFinger[ch1] == characterToFinger[ch2], "An SFB should always use the same fingers for both keys");

                        mResult.SfbsPerFinger[characterToFinger[ch1]] += bigramFreq[ch1, ch2];
                        mResult.SfsPerFinger[characterToFinger[ch1]] += skipgram2Freq[ch1, ch2];

                        mResult.SfbsPerHand[(int)FingerUtil.GetHandForFingerAsInt(characterToFinger[ch1])] += bigramFreq[ch1, ch2];
                        mResult.SfsPerHand[(int)FingerUtil.GetHandForFingerAsInt(characterToFinger[ch1])] += skipgram2Freq[ch1, ch2];
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void HandleCharSwap(byte ch1, byte ch2, List<(int, int)> ch1SameFingerMap, 
            int k1i, int k1j, int k2i, int k2j)
        {
            var kb = mKb.TransformedKbState;
            uint[,] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            uint[,] skipgram2Freq = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2Freq;

            for (int i = 0; i < ch1SameFingerMap.Count; i++)
            {
                var sameFingerPos = ch1SameFingerMap[i];
                long subCh1, subCh2, subCh3, subCh4;

                // If it is swapping with the other key.
                var otherCh = kb[sameFingerPos.Item1][sameFingerPos.Item2];
                if (otherCh == ch1)
                {
                    var distance = FingerUtil.MovementDistance[k1i, k1j, k2i, k2j];

                    otherCh = ch2;
                    subCh1 = bigramFreq[otherCh, ch1];
                    subCh2 = bigramFreq[ch1, otherCh];

                    mResult.TotalSfbs -= subCh1;
                    mResult.TotalSfbs += subCh2;

                    mResult.SfbDistancePerKey[k1i, k1j] -= (subCh2 / totalBg) * distance;
                    mResult.SfbDistancePerKey[k1i, k1j] += (subCh1 / totalBg) * distance;

                    subCh1 = skipgram2Freq[otherCh, ch1];
                    subCh2 = skipgram2Freq[ch1, otherCh];

                    mResult.SfsDistancePerKey[k1i, k1j] -= (subCh2 / totalBg) * distance;
                    mResult.SfsDistancePerKey[k1i, k1j] += (subCh1 / totalBg) * distance;

                    mResult.TotalSfs -= subCh1;
                    mResult.TotalSfs += subCh2;
                }
                else
                {
                    var distance = FingerUtil.MovementDistance[k1i, k1j, sameFingerPos.Item1, sameFingerPos.Item2];

                    subCh1 = bigramFreq[otherCh, ch1];
                    subCh2 = bigramFreq[ch1, otherCh];
                    subCh3 = bigramFreq[otherCh, ch2];
                    subCh4 = bigramFreq[ch2, otherCh];

                    mResult.TotalSfbs -= subCh1;
                    mResult.TotalSfbs -= subCh2;
                    mResult.TotalSfbs += subCh3;
                    mResult.TotalSfbs += subCh4;

                    // The distance[i, j] = the distance from that key to every other bigram.
                    mResult.SfbDistancePerKey[k1i, k1j] -= (subCh2 / totalBg) * distance;
                    mResult.SfbDistancePerKey[k1i, k1j] += (subCh4 / totalBg) * distance;
                    mResult.SfbDistancePerKey[sameFingerPos.Item1, sameFingerPos.Item2] -= (subCh1 / totalBg) * distance;
                    mResult.SfbDistancePerKey[sameFingerPos.Item1, sameFingerPos.Item2] += (subCh3 / totalBg) * distance;

                    // Now do sfs.
                    subCh1 = skipgram2Freq[otherCh, ch1];
                    subCh2 = skipgram2Freq[ch1, otherCh];
                    subCh3 = skipgram2Freq[otherCh, ch2];
                    subCh4 = skipgram2Freq[ch2, otherCh];

                    mResult.SfsDistancePerKey[k1i, k1j] -= (subCh2 / totalBg) * distance;
                    mResult.SfsDistancePerKey[k1i, k1j] += (subCh4 / totalBg) * distance;
                    mResult.SfsDistancePerKey[sameFingerPos.Item1, sameFingerPos.Item2] -= (subCh1 / totalBg) * distance;
                    mResult.SfsDistancePerKey[sameFingerPos.Item1, sameFingerPos.Item2] += (subCh3 / totalBg) * distance;

                    mResult.TotalSfs -= subCh1;
                    mResult.TotalSfs -= subCh2;
                    mResult.TotalSfs += subCh3;
                    mResult.TotalSfs += subCh4;
                }
            }
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            var sameFingerMap = mSfm.SameFingerKeysPerKey;

            // This list of keys will contain all the keys which were an sfb with key1 before the swap.
            // The keyboard state has already been swapped though, but the key positions don't change. The same finger map will be ch1s, but subtract ch1 (really at key2)
            var ch1SameFingerMap = sameFingerMap[k1i, k1j];
            var ch2SameFingerMap = sameFingerMap[k2i, k2j];
            var ch1 = mKb.TransformedKbState[k2i][k2j];
            var ch2 = mKb.TransformedKbState[k1i][k1j];

            HandleCharSwap(ch1, ch2, ch1SameFingerMap, k1i, k1j, k2i, k2j);
            HandleCharSwap(ch2, ch1, ch2SameFingerMap, k2i, k2j, k1i, k1j);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }

    public class SameFingerMapResult : AnalysisResult
    {
        public SameFingerMapResult(Enum resultId) : base(resultId)
        {
        }

        public List<(int, int)>[,] SameFingerKeysPerKey = new List<(int, int)>[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
    }

    /// <summary>
    /// Computes a map for each key where it includes a list of the keys which are on the same finger.
    /// Does not respond to key swaps.
    /// </summary>
    public class SameFingerMap : GraphNode
    {
        protected SameFingerMapResult mResult;

        public SameFingerMap() : base(eInputNodes.SameFingerMap)
        {
            AddInputNode(eInputNodes.KeyToFingerAsInt);
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            mResult = new SameFingerMapResult(this.NodeId);
            var keyToFinger = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var k2f = keyToFinger.KeyToFinger;

            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var key1Finger = k2f[i][j];

                    if(key1Finger == (int)eFinger.None)
                    {
                        continue;
                    }

                    mResult.SameFingerKeysPerKey[i, j] = new List<(int, int)>();

                    for(int k = 0; k < KeyboardStateSetting.ROWS; k++)
                    {
                        for(int w = 0; w < KeyboardStateSetting.COLS; w++)
                        {
                            if (i == k && j == w)
                            {
                                continue;
                            }

                            var key2Finger = k2f[k][w];

                            if(key1Finger == key2Finger)
                            {
                                mResult.SameFingerKeysPerKey[i, j].Add((k, w));
                            }
                        }
                    }
                }
            }
        }
    }

    public class FingerAsIntToHomePositionResult : AnalysisResult
    {
        public FingerAsIntToHomePositionResult(Enum resultId) : base(resultId)
        {
        }

        public (int, int)[] FingerToHomePosition { get; set; } = new (int, int)[Utils.GetTokens<eFinger>().Count()];
    }

    public class FingerAsIntToHomePosition : GraphNode
    {
        private FingerAsIntToHomePositionResult mResult;

        public FingerAsIntToHomePosition() : base(eInputNodes.FingerAsIntToHomePosition)
        {
            mResult = new FingerAsIntToHomePositionResult(this.NodeId);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            // Go through the entire keyboard state, and get the starting finger. If it's not none, set that finger to have row,col for the home pos.
            var hrMap = SettingState.FingerSettings.FingerHomePosition;

            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var homeFinger = hrMap.GetValueAt(i, j);

                    if (homeFinger != eFinger.None)
                    {
                        mResult.FingerToHomePosition[(int)homeFinger] = (i, j);
                    }
                }
            }

            // Set the thumb.
            mResult.FingerToHomePosition[(int)eFinger.RightThumb] = (3, 0);
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;
    }

    public class TransformedCharacterToFingerAsIntResult : AnalysisResult
    {
        public TransformedCharacterToFingerAsIntResult(Enum resultId) : base(resultId)
        {
        }

        public int[] CharacterToFinger { get; set; }
    }

    /// <summary>
    /// Maps a character (as int) to a key in the layout (row, col) form.
    /// </summary>
    public class TransformedCharacterToFingerAsInt : GraphNode
    {
        private TransformedCharacterToFingerAsIntResult mResult;

        public TransformedCharacterToFingerAsInt() : base(eInputNodes.TransformedCharacterToFingerAsInt)
        {
            mResult = new TransformedCharacterToFingerAsIntResult(this.NodeId);
            AddInputNode(eInputNodes.TransformedCharacterToKey);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
            AddInputNode(eInputNodes.TransfomedKbState);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private KeyToFingerAsIntResult mK2f;
        private TransformedKbStateResult mKb;

        protected override void Compute()
        {
            TransformedCharacterToKeyResult c2k = (TransformedCharacterToKeyResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            mK2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            mKb = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];

            // Defaulted to None.
            mResult.CharacterToFinger = new int[c2k.CharacterToKey.Length];

            for(int i = 0; i < c2k.CharacterToKey.Length; i++)
            {
                var k = c2k.CharacterToKey[i];
                if(k.Item1 == -1)
                {
                    continue;
                }

                mResult.CharacterToFinger[i] = mK2f.KeyToFinger[k.Item1][k.Item2];
            }
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            byte ch2 = mKb.TransformedKbState[k1i][k1j];
            byte ch1 = mKb.TransformedKbState[k2i][k2j];

            mResult.CharacterToFinger[ch1] = mK2f.KeyToFinger[k2i][k2j];
            mResult.CharacterToFinger[ch2] = mK2f.KeyToFinger[k1i][k1j];
        }
    }

    public class TransformedCharacterToKeyResult : AnalysisResult
    {
        public TransformedCharacterToKeyResult(Enum resultId) : base(resultId)
        {
        }

        public (int, int)[] CharacterToKey { get; set; }
    }

    /// <summary>
    /// Maps a character (as int) to a key in the layout (row, col) form.
    /// </summary>
    public class TransformedCharacterToKey : GraphNode
    {
        private TransformedCharacterToKeyResult mResult;
        private CharacterSetAsListResult mCharacterListResult;
        private TransformedKbStateResult mTransformedKbStateResult;

        public TransformedCharacterToKey() : base(eInputNodes.TransformedCharacterToKey)
        {
            mResult = new TransformedCharacterToKeyResult(this.NodeId);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.CharacterSetAsList);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            mCharacterListResult = (CharacterSetAsListResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.CharacterSetAsList];
            var characterSet = mCharacterListResult.CharacterSet;

            mTransformedKbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var kbs = mTransformedKbStateResult.TransformedKbState;

            // For every key in the characterset, map it to an index on the keyboard.
            mResult.CharacterToKey = new (int, int)[characterSet.Count()];

            for(int i = 0; i < mResult.CharacterToKey.Length; i++)
            {
                mResult.CharacterToKey[i] = (-1, -1);
            }

            for(int i = 0; i < kbs.Length; i++)
            {
                for(int j = 0; j < kbs[i].Length; j++)
                {
                    int k = kbs[i][j];
                    mResult.CharacterToKey[k] = (i, j);
                }
            }
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // The keyboard state has already changed, therefore the keys are already swapped.
            var ch2 = mTransformedKbStateResult.TransformedKbState[k1i][k1j];
            var ch1 = mTransformedKbStateResult.TransformedKbState[k2i][k2j];

            mResult.CharacterToKey[ch2] = (k1i, k1j);
            mResult.CharacterToKey[ch1] = (k2i, k2j);
        }
    }

    public class BigramClassificationResult : AnalysisResult
    {
        public enum eBigramClassification
        {
            // None means the bigram doesn't appear on the layout.
            None,
            // SFB is the only remaining case if none of the other ones here make sense.
            SFB,
            Alternation,
            InRoll,
            Outroll,
        }

        public BigramClassificationResult(Enum resultId) : base(resultId)
        {
        }

        public eBigramClassification[,] BigramClassifications { get; set; }
    }

    /// <summary>
    /// Classifies each bigram on the layout.
    /// </summary>
    public class BigramClassification : GraphNode
    {
        private BigramClassificationResult mResult;
        private TransformedCharacterToKeyResult mTransformedCharacterToKeyResult;
        private TransformedKbStateResult mTransformedKbStateResult;
        private KeyToFingerAsIntResult mKeyToFingerAsIntResult;

        public BigramClassification() :
            base(eInputNodes.BigramClassification)
        {
            mResult = new BigramClassificationResult(NodeId);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
            AddInputNode(eInputNodes.TransformedCharacterToKey);
            AddInputNode(eInputNodes.TransfomedKbState);
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            mTransformedCharacterToKeyResult = (TransformedCharacterToKeyResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            var charToKey = mTransformedCharacterToKeyResult.CharacterToKey;

            mKeyToFingerAsIntResult = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var keyToFinger = mKeyToFingerAsIntResult.KeyToFinger;

            mTransformedKbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];

            uint[,] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            mResult.BigramClassifications = new BigramClassificationResult.eBigramClassification[bigramFreq.GetLength(0), bigramFreq.GetLength(1)];

            int len = bigramFreq.GetLength(0);

            for(int i = 0; i < len; i++)
            {
                var k1Loc = charToKey[i];
                if (k1Loc.Item2 == -1)
                    continue;

                int k1Finger = keyToFinger[k1Loc.Item1][k1Loc.Item2];
                eHand k1Hand = FingerUtil.GetHandForFingerAsInt(k1Finger);
                LogUtils.Assert(k1Hand != eHand.None);

                for (int j = 0; j < len; j++)
                {
                    var k2Loc = charToKey[j];
                    if (k2Loc.Item1 == -1 || i == j)
                        continue;

                    int k2Finger = keyToFinger[k2Loc.Item1][k2Loc.Item2];
                    eHand k2Hand = FingerUtil.GetHandForFingerAsInt(k2Finger);
                    LogUtils.Assert(k2Hand != eHand.None);

                    if(k1Hand != k2Hand)
                    {
                        mResult.BigramClassifications[i, j] = BigramClassificationResult.eBigramClassification.Alternation;
                    }
                    else
                    {
                        if(k1Finger == k2Finger)
                        {
                            mResult.BigramClassifications[i, j] = BigramClassificationResult.eBigramClassification.SFB;
                        }
                        // If we are on the right hand, going from a lower index to a higher index is an outroll.
                        else if(k1Hand == eHand.Right)
                        {
                            if(k1Finger > k2Finger)
                            {
                                mResult.BigramClassifications[i, j] = BigramClassificationResult.eBigramClassification.InRoll;
                            }
                            else
                            {
                                mResult.BigramClassifications[i, j] = BigramClassificationResult.eBigramClassification.Outroll;
                            }
                        }
                        else
                        {
                            if(k1Finger > k2Finger)
                            {
                                mResult.BigramClassifications[i, j] = BigramClassificationResult.eBigramClassification.Outroll;
                            }
                            else
                            {
                                mResult.BigramClassifications[i, j] = BigramClassificationResult.eBigramClassification.InRoll;
                            }
                        }
                    }
                }
            }
        }
    }

    class CharacterSetAsListResult : AnalysisResult
    {
        public CharacterSetAsListResult(Enum id)
            : base(id)
        {
        }

        public List<char> CharacterSet { get; set; }
    }

    class CharacterSetAsList : GraphNode
    {
        protected CharacterSetAsListResult mResult;
        public CharacterSetAsList() : base(eInputNodes.CharacterSetAsList)
        {
        }

        protected override void Compute()
        {
            mResult = new CharacterSetAsListResult(this.NodeId);
            List<char> characterSet = SettingState.MeasurementSettings.CharFrequencyData.UsedCharset.ToList<char>();
            mResult.CharacterSet = characterSet;
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;
    }

    class KeyToFingerAsIntResult : AnalysisResult
    {
        public KeyToFingerAsIntResult(Enum id)
            : base(id)
        {
        }

        public int[][] KeyToFinger { get; set; }
    }

    class KeyToFingerAsInt : GraphNode
    {
        protected KeyToFingerAsIntResult mResult;

        public KeyToFingerAsInt() : base(eInputNodes.KeyToFingerAsInt)
        {
        }

        protected override void Compute()
        {
            mResult = new KeyToFingerAsIntResult(this.NodeId);

            var k2f = SettingState.FingerSettings.KeyMappings;

            int[][] keyToFingerInt = new int[KeyboardStateSetting.ROWS + 1][];

            // Add the space key.
            keyToFingerInt[KeyboardStateSetting.ROWS] = new int[1];
            keyToFingerInt[KeyboardStateSetting.ROWS][0] = (int)eFinger.RightThumb;

            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                keyToFingerInt[i] = new int[KeyboardStateSetting.COLS];
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    keyToFingerInt[i][j] = (int)k2f.GetValueAt(i, j);
                }
            }

            mResult.KeyToFinger = keyToFingerInt;
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;
    }

    class TransformedKbStateResult : AnalysisResult
    {
        public TransformedKbStateResult(Enum id)
            : base(id)
        {
        }

        public byte[][] TransformedKbState { get; set; }


        /// <summary>
        /// Just prints the kb state to the console log.
        /// </summary>
        public static void LogKbState(byte[][] kbState)
        {
            StringBuilder builder = new StringBuilder();
            string usedCharset = SettingState.MeasurementSettings.CharFrequencyData.UsedCharset;

            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    builder.Append($" {usedCharset[kbState[i][j]]}");
                }
            }

            LogUtils.LogInfo($"Found local optimum: {builder.ToString()}");
        }
    }

    /// <summary>
    /// The keyboard state transformed such that each character is an index into the frequency data.
    /// </summary>
    class TransformedKbState : GraphNode
    {
        protected TransformedKbStateResult mResult;
        protected CharacterSetAsListResult mCharacterSetAsList;

        public TransformedKbState() : base(eInputNodes.TransfomedKbState)
        {
            AddInputNode(eInputNodes.CharacterSetAsList);
        }

        protected override void Compute()
        {
            mResult = new TransformedKbStateResult(this.NodeId);

            mCharacterSetAsList = (CharacterSetAsListResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.CharacterSetAsList];
            var characterSet = mCharacterSetAsList.CharacterSet;
            var kbs = SettingState.KeyboardSettings.KeyboardState;

            // Just for fun, compute it here when in reality this should just trigger the analysis system to run.
            byte[][] transformedKeyState = new byte[KeyboardStateSetting.ROWS + 1][];
            transformedKeyState[KeyboardStateSetting.ROWS] = new byte[1];

            // Add space to map.
            int charIdx = characterSet.IndexOf(' ');
            transformedKeyState[KeyboardStateSetting.ROWS][0] = (byte)charIdx;

            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                transformedKeyState[i] = new byte[KeyboardStateSetting.COLS];
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    charIdx = characterSet.IndexOf(kbs.GetValueAt(i, j));
                    transformedKeyState[i][j] = (byte)charIdx;
                    LogUtils.Assert(charIdx > 0, "Character on keyboard not found in available charset.");
                }
            }

            mResult.TransformedKbState = transformedKeyState;
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // Very lightweight, just swap the keys in the array.
            byte temp = mResult.TransformedKbState[k1i][k1j];
            mResult.TransformedKbState[k1i][k1j] = mResult.TransformedKbState[k2i][k2j];
            mResult.TransformedKbState[k2i][k2j] = temp;
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }
}
