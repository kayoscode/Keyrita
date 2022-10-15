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
        KeyToFingerAsInt,
        TransfomedKbState
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
        protected CharacterSetAsListResult result;
        public CharacterSetAsList(Enum id) : base(id)
        {
        }

        public override void Compute()
        {
            result = new CharacterSetAsListResult(this.Op);
            List<char> characterSet = SettingState.MeasurementSettings.CharFrequencyData.UsedCharset.ToList<char>();
            result.CharacterSet = characterSet;
        }

        public override AnalysisResult GetResult()
        {
            return result;
        }
    }

    class KeyToFingerAsIntResult : AnalysisResult
    {
        public KeyToFingerAsIntResult(Enum id)
            : base(id)
        {
        }

        public int[,] KeyToFinger { get; set; }
    }

    class KeyToFingerAsInt : OperationBase
    {
        protected KeyToFingerAsIntResult result;

        public KeyToFingerAsInt(Enum id) : base(id)
        {
        }

        public override void Compute()
        {
            result = new KeyToFingerAsIntResult(this.Op);

            var k2f = SettingState.FingerSettings.KeyMappings;

            int[,] keyToFingerInt = new int[3, 10];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    keyToFingerInt[i, j] = (int)k2f.GetValueAt(i, j);
                }
            }

            result.KeyToFinger = keyToFingerInt;
        }

        public override AnalysisResult GetResult()
        {
            return result;
        }
    }

    class TransformedKbStateResult : AnalysisResult
    {
        public TransformedKbStateResult(Enum id)
            : base(id)
        {
        }

        public byte[,] TransformedKbState { get; set; }
    }

    class TransformedKbState : OperationBase
    {
        protected TransformedKbStateResult result;

        public TransformedKbState(Enum id) : base(id)
        {
            AddInputOp(eDependentOps.CharacterSetAsList);
        }

        public override void Compute()
        {
            result = new TransformedKbStateResult(this.Op);

            CharacterSetAsListResult characterListResult = (CharacterSetAsListResult)OperationSystem.ResolvedOps[eDependentOps.CharacterSetAsList];
            var characterSet = characterListResult.CharacterSet;
            var kbs = SettingState.KeyboardSettings.KeyboardState;

            // Just for fun, compute it here when in reality this should just trigger the analysis system to run.
            byte[,] transformedKeyState = new byte[3, 10];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    int charIdx = characterSet.IndexOf(kbs.GetValueAt(i, j));
                    transformedKeyState[i, j] = (byte)charIdx;
                    LTrace.Assert(charIdx > 0, "Character on keyboard not found in available charset.");
                }
            }

            result.TransformedKbState = transformedKeyState;
        }

        public override AnalysisResult GetResult()
        {
            return result;
        }
    }
}
