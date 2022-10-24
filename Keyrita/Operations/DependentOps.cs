using System;
using System.Collections.Generic;
using System.Linq;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Operations
{
    public enum eDependentOps
    {
        CharacterSetAsList,
        TransformedCharacterToKey,
        KeyToFingerAsInt,
        TransformedCharacterToFingerAsInt,
        TransfomedKbState,
        SameFingerMappings,
        BigramClassification,
        TrigramStats
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
    public class TransformedCharacterToFingerAsInt : OperationBase
    {
        private TransformedCharacterToFingerAsIntResult mResult;

        public TransformedCharacterToFingerAsInt() : base(eDependentOps.TransformedCharacterToFingerAsInt)
        {
            mResult = new TransformedCharacterToFingerAsIntResult(this.Op);
            AddInputOp(eDependentOps.TransformedCharacterToKey);
            AddInputOp(eDependentOps.KeyToFingerAsInt);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TransformedCharacterToKeyResult c2k = (TransformedCharacterToKeyResult)OperationSystem.ResolvedOps[eDependentOps.TransformedCharacterToKey];
            KeyToFingerAsIntResult k2f = (KeyToFingerAsIntResult)OperationSystem.ResolvedOps[eDependentOps.KeyToFingerAsInt];

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
    public class TransformedCharacterToKey : OperationBase
    {
        private TransformedCharacterToKeyResult mResult;

        public TransformedCharacterToKey() : base(eDependentOps.TransformedCharacterToKey)
        {
            mResult = new TransformedCharacterToKeyResult(this.Op);
            AddInputOp(eDependentOps.TransfomedKbState);
            AddInputOp(eDependentOps.CharacterSetAsList);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            CharacterSetAsListResult characterListResult = (CharacterSetAsListResult)OperationSystem.ResolvedOps[eDependentOps.CharacterSetAsList];
            var characterSet = characterListResult.CharacterSet;

            TransformedKbStateResult transformedKbState = (TransformedKbStateResult)OperationSystem.ResolvedOps[eDependentOps.TransfomedKbState];
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
    public class BigramClassification : OperationBase
    {
        BigramClassificationResult mResult;

        public BigramClassification() :
            base(eDependentOps.BigramClassification)
        {
            mResult = new BigramClassificationResult(this.Op);
            AddInputOp(eDependentOps.KeyToFingerAsInt);
            AddInputOp(eDependentOps.TransformedCharacterToKey);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TransformedCharacterToKeyResult c2k = (TransformedCharacterToKeyResult)OperationSystem.ResolvedOps[eDependentOps.TransformedCharacterToKey];
            var charToKey = c2k.CharacterToKey;

            KeyToFingerAsIntResult k2f = (KeyToFingerAsIntResult)OperationSystem.ResolvedOps[eDependentOps.KeyToFingerAsInt];
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
                LTrace.Assert(k1Hand != eHand.None);

                for (int j = 0; j < len; j++)
                {
                    var k2Loc = charToKey[j];
                    if (k2Loc.Item1 == -1 || i == j)
                        continue;

                    int k2Finger = keyToFinger[k2Loc.Item1][k2Loc.Item2];
                    eHand k2Hand = FingerUtil.GetHandForFingerAsInt(k2Finger);
                    LTrace.Assert(k2Hand != eHand.None);

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

    class CharacterSetAsList : OperationBase
    {
        protected CharacterSetAsListResult mResult;
        public CharacterSetAsList() : base(eDependentOps.CharacterSetAsList)
        {
        }

        protected override void Compute()
        {
            mResult = new CharacterSetAsListResult(this.Op);
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

    class KeyToFingerAsInt : OperationBase
    {
        protected KeyToFingerAsIntResult mResult;

        public KeyToFingerAsInt() : base(eDependentOps.KeyToFingerAsInt)
        {
        }

        protected override void Compute()
        {
            mResult = new KeyToFingerAsIntResult(this.Op);

            var k2f = SettingState.FingerSettings.KeyMappings;

            int[][] keyToFingerInt = new int[KeyboardStateSetting.ROWS + 1][];

            // Add the space key.
            keyToFingerInt[KeyboardStateSetting.ROWS] = new int[1];
            keyToFingerInt[KeyboardStateSetting.ROWS][0] = (int)eFinger.LeftThumb;

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
    class TransformedKbState : OperationBase
    {
        protected TransformedKbStateResult mResult;

        public TransformedKbState() : base(eDependentOps.TransfomedKbState)
        {
            AddInputOp(eDependentOps.CharacterSetAsList);
        }

        protected override void Compute()
        {
            mResult = new TransformedKbStateResult(this.Op);

            CharacterSetAsListResult characterListResult = (CharacterSetAsListResult)OperationSystem.ResolvedOps[eDependentOps.CharacterSetAsList];
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
                    LTrace.Assert(charIdx > 0, "Character on keyboard not found in available charset.");
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
