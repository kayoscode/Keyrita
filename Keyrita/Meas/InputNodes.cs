using System;
using System.Collections.Generic;
using System.Linq;
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
        SameFingerMappings,
        BigramClassification,
        TrigramStats,
        FingerAsIntToHomePosition
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
