using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
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
        KeySpeed,
    }

    /// <summary>
    /// Given a key on the keyboard, find a weighted sum of its same finger bigrams and 
    /// </summary>
    public class KeySpeedResult : PerKeyAnalysisResult
    {
        public KeySpeedResult(Enum resultId) : base(resultId)
        {
        }
    }

    public class KeySpeed : GraphNode
    {
        // These all need to eventually be settings.
        private const double SFB_WEIGHT = 1000;
        private const double SFS_WEIGHT = 500;
        private const double SCISSOR_WEIGHT = 1700;

        // Penalty applied to key scores just for existing at their location :D
        // Eventually let the user set these and discriminate their own way!
        private static double[,] KEY_LOCATION_PENALTY = new double[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS]
        {
            { 1.7, 1.25, 1.2, 1.1, 1.05, 1.05, 1.1, 1.2, 1.25, 1.7 },
            { 1.4, 1.2, 1.05, 1.0, 1.2, 1.2, 1.0, 1.05, 1.2, 1.4 },
            { 1.7, 1.45, 1.25, 1.1, 1.3, 1.3, 1.1, 1.25, 1.45, 1.7 },
        };

        private KeySpeedResult mResult;
        public KeySpeed() :
            base(eInputNodes.KeySpeed)
        {
            mResult = new KeySpeedResult(NodeId);
            AddInputNode(eInputNodes.TwoFingerStats);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
            AddInputNode(eMeasurements.Scissors);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            mResult.Clear();
            var k2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var sameFingerStats = (TwoFingerStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TwoFingerStats];
            var scissorsStats = (ScissorsResult)AnalysisGraphSystem.ResolvedNodes[eMeasurements.Scissors];
            var scissorValues = scissorsStats.PerKeyResult;
            var sfbsPerKey = sameFingerStats.SfbDistancePerKey;
            var sfsPerKey = sameFingerStats.SfsDistancePerKey;
            var keyToFinger = k2f.KeyToFinger;

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
                    score += scissorValues[i, j] * SCISSOR_WEIGHT;

                    score *= fingerWeight;
                    score *= KEY_LOCATION_PENALTY[i, j]; // Sucks to suck.
                    mResult.PerKeyResult[i, j] = score;
                }
            }
        }
    }

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
            AddInputNode(eInputNodes.BigramClassification);
            AddInputNode(eInputNodes.TransformedCharacterToFingerAsInt);
            AddInputNode(eInputNodes.TransformedCharacterToKey);
        }

        protected override void Compute()
        {
            mResult = new TwoFingerStatsResult(NodeId);

            BigramClassificationResult bgc = (BigramClassificationResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.BigramClassification];
            TransformedCharacterToFingerAsIntResult c2f = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            TransformedCharacterToKeyResult c2k = (TransformedCharacterToKeyResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToKey];

            uint[,] bigramFreq = SettingState.MeasurementSettings.CharFrequencyData.BigramFreq;
            double totalBg = SettingState.MeasurementSettings.CharFrequencyData.BigramHitCount;
            uint[,] skipgram2Freq = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2Freq;
            double totalSkipgram2 = SettingState.MeasurementSettings.CharFrequencyData.Skipgram2HitCount;

            var bigramClassifications = bgc.BigramClassifications;
            var characterToFinger = c2f.CharacterToFinger;
            var characterToKey = c2k.CharacterToKey;

            for(int i = 0; i < bigramClassifications.GetLength(0); i++)
            {
                for(int j = 0; j < bigramClassifications.GetLength(1); j++)
                {
                    if (bigramClassifications[i, j] == BigramClassificationResult.eBigramClassification.SFB)
                    {
                        var firstCharPos = characterToKey[i];
                        var secondCharPos = characterToKey[j];
                        var distance = FingerUtil.MovementDistance[firstCharPos.Item1, firstCharPos.Item2, secondCharPos.Item1, secondCharPos.Item2];
                        LogUtils.Assert(i != j, "An SFB should never be the same key");

                        // Add same finger stats for the bigram. Because we know that each bigram which is classified as an sfb will be hit with the same finger, it doesn't
                        // matter if we are searching for sfbs, sfs, or skipgrams(n). Just make sure to use the correct frequency data.
                        mResult.TotalSfbs += bigramFreq[i, j];
                        mResult.SfbDistancePerKey[firstCharPos.Item1, firstCharPos.Item2] += (bigramFreq[i, j] / totalBg) * distance;
                        mResult.TotalSfs += skipgram2Freq[i, j];
                        mResult.SfsDistancePerKey[firstCharPos.Item1, firstCharPos.Item2] += (skipgram2Freq[i, j] / totalSkipgram2) * distance;

                        // i and j have the same finger.
                        LogUtils.Assert(characterToFinger[i] == characterToFinger[j], "An SFB should always use the same fingers for both keys");

                        mResult.SfbsPerFinger[characterToFinger[i]] += bigramFreq[i, j];
                        mResult.SfsPerFinger[characterToFinger[i]] += skipgram2Freq[i, j];

                        mResult.SfbsPerHand[(int)FingerUtil.GetHandForFingerAsInt(characterToFinger[i])] += bigramFreq[i, j];
                        mResult.SfsPerHand[(int)FingerUtil.GetHandForFingerAsInt(characterToFinger[i])] += skipgram2Freq[i, j];
                    }
                }
            }
        }

        public override AnalysisResult GetResult()
        {

            return mResult;
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
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TransformedCharacterToKeyResult c2k = (TransformedCharacterToKeyResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            KeyToFingerAsIntResult k2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];

            // Defaulted to None.
            mResult.CharacterToFinger = new int[c2k.CharacterToKey.Length];

            for(int i = 0; i < c2k.CharacterToKey.Length; i++)
            {
                var k = c2k.CharacterToKey[i];
                if(k.Item1 == -1)
                {
                    continue;
                }

                mResult.CharacterToFinger[i] = k2f.KeyToFinger[k.Item1][k.Item2];
            }
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
            CharacterSetAsListResult characterListResult = (CharacterSetAsListResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.CharacterSetAsList];
            var characterSet = characterListResult.CharacterSet;

            TransformedKbStateResult transformedKbState = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var kbs = transformedKbState.TransformedKbState;

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
        BigramClassificationResult mResult;

        public BigramClassification() :
            base(eInputNodes.BigramClassification)
        {
            mResult = new BigramClassificationResult(this.NodeId);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
            AddInputNode(eInputNodes.TransformedCharacterToKey);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TransformedCharacterToKeyResult c2k = (TransformedCharacterToKeyResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            var charToKey = c2k.CharacterToKey;

            KeyToFingerAsIntResult k2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var keyToFinger = k2f.KeyToFinger;

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
    /// @TODO: see if we can optimize this.
    /// </summary>
    class TransformedKbState : GraphNode
    {
        protected TransformedKbStateResult mResult;

        public TransformedKbState() : base(eInputNodes.TransfomedKbState)
        {
            AddInputNode(eInputNodes.CharacterSetAsList);
        }

        protected override void Compute()
        {
            mResult = new TransformedKbStateResult(this.NodeId);

            CharacterSetAsListResult characterListResult = (CharacterSetAsListResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.CharacterSetAsList];
            var characterSet = characterListResult.CharacterSet;
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

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }
}
