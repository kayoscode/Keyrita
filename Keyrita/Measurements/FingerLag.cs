using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Measurements
{
    public class FingerLagResult : SFBResult
    {
        public FingerLagResult(Enum resultId) 
            : base(resultId)
        {
        }
    }

    /// <summary>
    /// Gives the speed of each finger. Just a sum of the key speed but on each finger.
    /// </summary>
    public class FingerLag : FingerHandMeasurement
    {
        protected FingerLagResult mResult;

        public FingerLag() : base(eMeasurements.FingerLag)
        {
            AddInputNode(eInputNodes.KeyLag);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
        }

        protected override void Compute()
        {
            mResult = new FingerLagResult(NodeId);

            var ks = (KeyLagResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyLag];
            var keyLag = ks.PerKeyResult;

            var k2f = (KeyToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var keyToFinger = k2f.KeyToFinger;

            // Go through each key on the keyboard and get the finger used for it.
            // Then add the result to the sum. Higher finger speeds are worse.
            for(int i = 0; i < keyLag.GetLength(0); i++)
            {
                for(int j = 0; j < keyLag.GetLength(1); j++)
                {
                    var finger = keyToFinger[i][j];
                    var hand = FingerUtil.GetHandForFingerAsInt(finger);

                    mResult.PerFingerResult[finger] += keyLag[i, j];
                    mResult.PerHandResult[(int)hand] += keyLag[i, j];
                    mResult.TotalResult += keyLag[i, j];
                }
            }

            // Set meas results.
            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                SetFingerResult(finger, mResult.PerFingerResult[(int)finger]);
                resultIdx++;
            }

            SetLeftHandResult(mResult.PerHandResult[(int)eHand.Left]);
            SetRightHandResult(mResult.PerHandResult[(int)eHand.Right]);
            SetTotalResult(mResult.TotalResult);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }
    }
}
