using System;
using Keyrita.Analysis;
using Keyrita.Analysis.AnalysisUtil;
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

        public FingerLag(AnalysisGraph graph) : base(eMeasurements.FingerLag, graph)
        {
            AddInputNode(eInputNodes.KeyLag);
            AddInputNode(eInputNodes.KeyToFingerAsInt);
        }

        protected override void Compute()
        {
            mResult = new FingerLagResult(NodeId);

            var ks = (KeyLagResult)AnalysisGraph.ResolvedNodes[eInputNodes.KeyLag];
            var keyLag = ks.PerKeyResult;

            var k2f = (KeyToFingerAsIntResult)AnalysisGraph.ResolvedNodes[eInputNodes.KeyToFingerAsInt];
            var keyToFinger = k2f.KeyToFinger;

            // Go through each key on the keyboard and get the finger used for it.
            // Then add the result to the sum. Higher finger speeds are worse.
            for(int i = 0; i < keyLag.Length; i++)
            {
                for(int j = 0; j < keyLag[i].Length; j++)
                {
                    var finger = keyToFinger[i][j];
                    var hand = FingerUtil.GetHandForFingerAsInt(finger);

                    mResult.PerFingerResult[finger] += keyLag[i][j];
                    mResult.PerHandResult[(int)hand] += keyLag[i][j];
                }
            }

            // Set meas results.
            int resultIdx = 0;
            foreach(eFinger finger in Utils.GetTokens<eFinger>())
            {
                SetFingerResult(finger, mResult.PerFingerResult[(int)finger]);
                resultIdx++;
            }

            mResult.TotalResult = ks.TotalResult;
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
