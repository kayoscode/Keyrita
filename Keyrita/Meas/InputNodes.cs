using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing.IndexedProperties;
using System.Runtime.CompilerServices;
using Keyrita.Measurements;
using Keyrita.Analysis.AnalysisUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Analysis
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
        public double EffortMapResult { get; set; }
    }

    public class KeyLag : GraphNode
    {
        // These all need to eventually be settings.
        private const double SFB_WEIGHT = 890;
        private const double SFS_WEIGHT = 135;
        private const double SCISSOR_WEIGHT = 800;
        private const double EFFORT_WEIGHT = 45;

        private KeyLagResult mResult;
        private KeyLagResult mResultBeforeSwap;

        public KeyLag(AnalysisGraph graph) :
            base(eInputNodes.KeyLag, graph)
        {
            AddInputNode(eInputNodes.TwoFingerStats);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
            AddInputNode(eInputNodes.ScissorsIntermediate);
            AddInputNode(eInputNodes.TransfomedKbState);

            mResult = new KeyLagResult(NodeId);
            mResultBeforeSwap = new KeyLagResult(NodeId);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        private KeyToFingerAsIntResult mK2f;
        private TwoFingerStatsResult mTfs;
        private ScissorsResult mSr;
        private TransformedKbStateResult mKbState;

        protected override void Compute()
        {
            mK2f = (KeyToFingerAsIntResult)AnalysisGraph.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            mTfs = (TwoFingerStatsResult)AnalysisGraph.ResolvedNodes[eInputNodes.TwoFingerStats];
            mSr = (ScissorsResult)AnalysisGraph.ResolvedNodes[eInputNodes.ScissorsIntermediate];
            mKbState = (TransformedKbStateResult)AnalysisGraph.ResolvedNodes[eInputNodes.TransfomedKbState];
            ComputeResult();
        }

        private void ComputeResult()
        {
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;

            mResult.TotalResult = 0;

            mResult.TotalResult = mTfs.TotalSfbDistance * SFB_WEIGHT;
            mResult.TotalResult += mTfs.TotalSfsDistance * SFS_WEIGHT;
            mResult.TotalResult += mSr.TotalWeightedResult * SCISSOR_WEIGHT;

            mResult.EffortMapResult = 0;

            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    mResult.EffortMapResult += (SettingState.MeasurementSettings.CharFrequencyData.CharFreq[mKbState.TransformedKbState[i][j]] / totalBg) * 
                        SettingState.FingerSettings.EffortMap.GetValueAt(i, j);
                }
            }

            mResult.TotalResult += mResult.EffortMapResult * EFFORT_WEIGHT;
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            mResultBeforeSwap.TotalResult = mResult.TotalResult;
            Compute();
        }

        public override void SwapBack()
        {
            mResult.TotalResult = mResultBeforeSwap.TotalResult;
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
        public double TotalSfbDistance { get; set; }
        public long[] SfbsPerHand { get; private set; } = new long[Utils.GetTokens<eHand>().Count()];
        public long[] SfbsPerFinger { get; private set; } = new long[Utils.GetTokens<eFinger>().Count()];
        public double[][] SfbDistancePerKey { get; private set; } = new double[KeyboardStateSetting.ROWS][]
        {
            new double[KeyboardStateSetting.COLS],
            new double[KeyboardStateSetting.COLS],
            new double[KeyboardStateSetting.COLS],
        };

        public long TotalSfs { get; set; }
        public double TotalSfsDistance { get; set; }
        public long[] SfsPerHand { get; private set; } = new long[Utils.GetTokens<eHand>().Count()];
        public long[] SfsPerFinger { get; private set; } = new long[Utils.GetTokens<eFinger>().Count()];
        public double[][] SfsDistancePerKey { get; private set; } = new double[KeyboardStateSetting.ROWS][]
        {
            new double[KeyboardStateSetting.COLS],
            new double[KeyboardStateSetting.COLS],
            new double[KeyboardStateSetting.COLS],
        };
    }

    /// <summary>
    /// Computes frequencies related to which keys are on which same fingers.
    /// These results will be used downstream for various measurements. The results may be directly output in some measurements.
    /// </summary>
    public class TwoFingerStats : GraphNode
    {
        protected TwoFingerStatsResult mResult;
        protected TwoFingerStatsResult mResultBeforeSwap;

        public TwoFingerStats(AnalysisGraph graph) : base(eInputNodes.TwoFingerStats, graph)
        {
            AddInputNode(eInputNodes.KeyToFingerAsInt);
            AddInputNode(eInputNodes.TransfomedKbState);
            AddInputNode(eInputNodes.SameFingerMap);

            mResultBeforeSwap = new TwoFingerStatsResult(this.NodeId);
        }

        private KeyToFingerAsIntResult mK2f;
        private TransformedKbStateResult mKb;
        private SameFingerMapResult mSfm;

        protected override void Compute()
        {
            mResult = new TwoFingerStatsResult(NodeId);

            mK2f = (KeyToFingerAsIntResult)AnalysisGraph.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            mKb = (TransformedKbStateResult)AnalysisGraph.ResolvedNodes[eInputNodes.TransfomedKbState];
            mSfm = (SameFingerMapResult)AnalysisGraph.ResolvedNodes[eInputNodes.SameFingerMap];

            uint[][] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            uint[][] skipgram2Freq = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2Freq;
            double totalSkipgram2 = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2HitCount;

            var keyToFinger = mK2f.KeyToFinger;
            var sameFingerMap = mSfm.SameFingerKeysPerKey;

            // Loop over all the keys, and get its same finger map.
            for(int i = 0; i < sameFingerMap.Length; i++)
            {
                for(int j = 0; j < sameFingerMap[i].Length; j++)
                {
                    var thisKeySfbMap = sameFingerMap[i][j];
                    var finger = keyToFinger[i][j];
                    var fingerWeight = SettingState.FingerSettings.FingerWeights.GetValueAt(finger);

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
                        mResult.TotalSfbs += bigramFreq[ch1][ch2];
                        double sfbDistance = ((bigramFreq[ch1][ch2] / totalBg) * distance) * fingerWeight;
                        mResult.SfbDistancePerKey[i][j] += sfbDistance;
                        mResult.TotalSfbDistance += sfbDistance;

                        mResult.TotalSfs += skipgram2Freq[ch1][ch2];
                        double sfsDistance = ((skipgram2Freq[ch1][ch2] / totalSkipgram2) * distance) * fingerWeight;
                        mResult.SfsDistancePerKey[i][j] += sfsDistance;
                        mResult.TotalSfsDistance += sfsDistance;

                        // i and j have the same finger.
                        LogUtils.Assert(keyToFinger[i][j] == keyToFinger[secondCharPos.Item1][secondCharPos.Item2], "An SFB should always use the same fingers for both keys");

                        mResult.SfbsPerFinger[keyToFinger[i][j]] += bigramFreq[ch1][ch2];
                        mResult.SfsPerFinger[keyToFinger[i][j]] += skipgram2Freq[ch1][ch2];

                        mResult.SfbsPerHand[(int)FingerUtil.GetHandForFingerAsInt(keyToFinger[i][j])] += bigramFreq[ch1][ch2];
                        mResult.SfsPerHand[(int)FingerUtil.GetHandForFingerAsInt(keyToFinger[i][j])] += skipgram2Freq[ch1][ch2];
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void HandleCharSwap(byte ch1, byte ch2, List<(int, int)> ch1SameFingerMap, 
            int k1i, int k1j, int k2i, int k2j)
        {
            var kb = mKb.TransformedKbState;
            uint[][] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            uint[][] skipgram2Freq = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2Freq;

            var finger = mK2f.KeyToFinger[k1i][k1j];
            var fingerWeight = SettingState.FingerSettings.FingerWeights.GetValueAt(finger);

            for (int i = 0; i < ch1SameFingerMap.Count; i++)
            {
                var sameFingerPos = ch1SameFingerMap[i];
                long subCh1, subCh2, subCh3, subCh4;

                // If it is swapping with the other key.
                var otherCh = kb[sameFingerPos.Item1][sameFingerPos.Item2];
                if (otherCh == ch1)
                {
                    var distance = FingerUtil.MovementDistance[k1i, k1j, k2i, k2j] * fingerWeight;

                    otherCh = ch2;
                    subCh1 = bigramFreq[otherCh][ch1];
                    subCh2 = bigramFreq[ch1][otherCh];

                    mResult.TotalSfbs -= subCh1;
                    mResult.TotalSfbs += subCh2;

                    subCh1 = skipgram2Freq[otherCh][ch1];
                    subCh2 = skipgram2Freq[ch1][otherCh];

                    mResult.TotalSfs -= subCh1;
                    mResult.TotalSfs += subCh2;
                }
                else
                {
                    var finger2 = mK2f.KeyToFinger[sameFingerPos.Item1][sameFingerPos.Item2];
                    var fingerWeight2 = SettingState.FingerSettings.FingerWeights.GetValueAt(finger2);

                    var baseDistance = FingerUtil.MovementDistance[k1i, k1j, sameFingerPos.Item1, sameFingerPos.Item2];
                    var distance = baseDistance * fingerWeight;
                    var distance2 = baseDistance * fingerWeight2;

                    subCh1 = bigramFreq[otherCh][ch1];
                    subCh2 = bigramFreq[ch1][otherCh];
                    subCh3 = bigramFreq[otherCh][ch2];
                    subCh4 = bigramFreq[ch2][otherCh];

                    // The distance[i, j] = the distance from that key to every other bigram.
                    mResult.TotalSfbDistance -= (subCh2 / totalBg) * distance;
                    mResult.TotalSfbDistance += (subCh4 / totalBg) * distance;
                    mResult.TotalSfbDistance -= (subCh1 / totalBg) * distance2;
                    mResult.TotalSfbDistance += (subCh3 / totalBg) * distance2;

                    // Now do sfs.
                    subCh1 = skipgram2Freq[otherCh][ch1];
                    subCh2 = skipgram2Freq[ch1][otherCh];
                    subCh3 = skipgram2Freq[otherCh][ch2];
                    subCh4 = skipgram2Freq[ch2][otherCh];

                    mResult.TotalSfsDistance -= (subCh2 / totalBg) * distance;
                    mResult.TotalSfsDistance += (subCh4 / totalBg) * distance;
                    mResult.TotalSfsDistance -= (subCh1 / totalBg) * distance2;
                    mResult.TotalSfsDistance += (subCh3 / totalBg) * distance2;
                }
            }
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // The previous swap event only cares about the total sfs distance and sfb distance.
            mResultBeforeSwap.TotalSfbDistance = mResult.TotalSfbDistance;
            mResultBeforeSwap.TotalSfsDistance = mResult.TotalSfsDistance;

            var sameFingerMap = mSfm.SameFingerKeysPerKey;

            // This list of keys will contain all the keys which were an sfb with key1 before the swap.
            // The keyboard state has already been swapped though, but the key positions don't change. The same finger map will be ch1s, but subtract ch1 (really at key2)
            var ch1SameFingerMap = sameFingerMap[k1i][k1j];
            var ch2SameFingerMap = sameFingerMap[k2i][k2j];
            var ch1 = mKb.TransformedKbState[k2i][k2j];
            var ch2 = mKb.TransformedKbState[k1i][k1j];

            HandleCharSwap(ch1, ch2, ch1SameFingerMap, k1i, k1j, k2i, k2j);
            HandleCharSwap(ch2, ch1, ch2SameFingerMap, k2i, k2j, k1i, k1j);
        }

        public override void SwapBack()
        {
            mResult.TotalSfbDistance = mResultBeforeSwap.TotalSfbDistance;
            mResult.TotalSfsDistance = mResultBeforeSwap.TotalSfsDistance;
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

        public List<(int, int)>[][] SameFingerKeysPerKey = new List<(int, int)>[KeyboardStateSetting.ROWS][]
        {
            new List<(int, int)>[KeyboardStateSetting.COLS],
            new List<(int, int)>[KeyboardStateSetting.COLS],
            new List<(int, int)>[KeyboardStateSetting.COLS],
        };
    }

    /// <summary>
    /// Computes a map for each key where it includes a list of the keys which are on the same finger.
    /// Does not respond to key swaps.
    /// </summary>
    public class SameFingerMap : GraphNode
    {
        protected SameFingerMapResult mResult;

        public SameFingerMap(AnalysisGraph graph) : base(eInputNodes.SameFingerMap, graph)
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
            var keyToFinger = (KeyToFingerAsIntResult)AnalysisGraph.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
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

                    mResult.SameFingerKeysPerKey[i][j] = new List<(int, int)>();

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
                                mResult.SameFingerKeysPerKey[i][j].Add((k, w));
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

        public FingerAsIntToHomePosition(AnalysisGraph graph) : base(eInputNodes.FingerAsIntToHomePosition, graph)
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
            mResult.FingerToHomePosition[(int)(eFinger)SettingState.MeasurementSettings.SpaceFinger.Value] = (3, 0);
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

        public TransformedCharacterToFingerAsInt(AnalysisGraph graph) : base(eInputNodes.TransformedCharacterToFingerAsInt, graph)
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
            TransformedCharacterToKeyResult c2k = (TransformedCharacterToKeyResult)AnalysisGraph.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            mK2f = (KeyToFingerAsIntResult)AnalysisGraph.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            mKb = (TransformedKbStateResult)AnalysisGraph.ResolvedNodes[eInputNodes.TransfomedKbState];

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

            mPreviousSwap1i = k1i;
            mPreviousSwap1j = k1j;
            mPreviousSwap2i = k2i;
            mPreviousSwap2j = k2j;
        }

        private int mPreviousSwap1i;
        private int mPreviousSwap1j;
        private int mPreviousSwap2i;
        private int mPreviousSwap2j;
        public override void SwapBack()
        {
            SwapKeys(mPreviousSwap1i, mPreviousSwap1j, mPreviousSwap2i, mPreviousSwap2j);
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

        public TransformedCharacterToKey(AnalysisGraph graph) : base(eInputNodes.TransformedCharacterToKey, graph)
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
            mCharacterListResult = (CharacterSetAsListResult)AnalysisGraph.ResolvedNodes[eInputNodes.CharacterSetAsList];
            var characterSet = mCharacterListResult.CharacterSet;

            mTransformedKbStateResult = (TransformedKbStateResult)AnalysisGraph.ResolvedNodes[eInputNodes.TransfomedKbState];
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

            mPreviousSwap1i = k1i;
            mPreviousSwap1j = k1j;
            mPreviousSwap2i = k2i;
            mPreviousSwap2j = k2j;
        }

        private int mPreviousSwap1i;
        private int mPreviousSwap1j;
        private int mPreviousSwap2i;
        private int mPreviousSwap2j;
        public override void SwapBack()
        {
            SwapKeys(mPreviousSwap1i, mPreviousSwap1j, mPreviousSwap2i, mPreviousSwap2j);
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
        public CharacterSetAsList(AnalysisGraph graph) : base(eInputNodes.CharacterSetAsList, graph)
        {
        }

        protected override void Compute()
        {
            mResult = new CharacterSetAsListResult(this.NodeId);
            List<char> characterSet = SettingState.MeasurementSettings.CharFrequencyData.AvailableCharSet.ToList<char>();
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

        public KeyToFingerAsInt(AnalysisGraph graph) : base(eInputNodes.KeyToFingerAsInt, graph)
        {
        }

        protected override void Compute()
        {
            mResult = new KeyToFingerAsIntResult(this.NodeId);

            var k2f = SettingState.FingerSettings.KeyMappings;

            int[][] keyToFingerInt = new int[KeyboardStateSetting.ROWS + 1][];

            // Add the space key.
            keyToFingerInt[KeyboardStateSetting.ROWS] = new int[1];
            keyToFingerInt[KeyboardStateSetting.ROWS][0] = (int)(eFinger)SettingState.MeasurementSettings.SpaceFinger.Value;

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
    }

    /// <summary>
    /// The keyboard state transformed such that each character is an index into the frequency data.
    /// </summary>
    class TransformedKbState : GraphNode
    {
        protected TransformedKbStateResult mResult;
        protected CharacterSetAsListResult mCharacterSetAsList;

        public TransformedKbState(AnalysisGraph graph) : base(eInputNodes.TransfomedKbState, graph)
        {
            AddInputNode(eInputNodes.CharacterSetAsList);
        }

        protected override void Compute()
        {
            mResult = new TransformedKbStateResult(this.NodeId);

            mCharacterSetAsList = (CharacterSetAsListResult)AnalysisGraph.ResolvedNodes[eInputNodes.CharacterSetAsList];
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

            mPreviousSwap1i = k1i;
            mPreviousSwap1j = k1j;
            mPreviousSwap2i = k2i;
            mPreviousSwap2j = k2j;
        }

        private int mPreviousSwap1i;
        private int mPreviousSwap1j;
        private int mPreviousSwap2i;
        private int mPreviousSwap2j;
        public override void SwapBack()
        {
            SwapKeys(mPreviousSwap1i, mPreviousSwap1j, mPreviousSwap2i, mPreviousSwap2j);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }
}
