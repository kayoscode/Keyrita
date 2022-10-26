using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Computes the rolls found in the keyboard layout.
    /// </summary>
    public class RollResult : AnalysisResult
    {
        public RollResult(Enum resultId) 
            : base(resultId)
        {
        }

        public double TotalRolls { get; set; }
        public double InRolls { get; set; }
        public double OutRolls { get; set; }
    }

    class Rolls : DynamicMeasurement
    {
        private RollResult mResult;

        public Rolls() : base(eMeasurements.Rolls)
        {
            mResult = new RollResult(this.NodeId);
            AddInputNode(eInputNodes.TrigramStats);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TrigramStatsResult tgs = (TrigramStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TrigramStats];
            mResult.TotalRolls = tgs.TotalRolls;
            mResult.InRolls = tgs.InRolls;
            mResult.OutRolls = tgs.OutRolls;

            SetResult(0, mResult.TotalRolls);
            SetResult(1, mResult.InRolls);
            SetResult(2, mResult.OutRolls);
        }
    }
}
