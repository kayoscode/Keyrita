using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Computes the rolls found in the keyboard layout.
    /// </summary>
    public class OneHandsResult : AnalysisResult
    {
        public OneHandsResult(Enum resultId) 
            : base(resultId)
        {
        }

        public double TotalOneHands { get; set; }
    }

    public class OneHands : DynamicMeasurement
    {
        private OneHandsResult mResult;

        public OneHands() : base(eMeasurements.OneHands)
        {
            mResult = new OneHandsResult(NodeId);
            AddInputNode(eInputNodes.TrigramStats);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TrigramStatsResult tgs = (TrigramStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TrigramStats];
            mResult.TotalOneHands = tgs.TotalOneHands;

            SetResult(0, mResult.TotalOneHands);
        }
    }
}
