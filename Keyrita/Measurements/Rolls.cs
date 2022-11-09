using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;

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
            AddInputNode(eInputNodes.SortedTrigramSet);
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            TrigramStatsResult tgs = (TrigramStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TrigramStats];

            SortedTrigramSetResult tgSet = (SortedTrigramSetResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.SortedTrigramSet];
            double totalTgs = tgSet.TrigramCoverage;

            mResult.TotalRolls = tgs.TotalRolls / totalTgs * 100;
            mResult.InRolls = tgs.InRolls / totalTgs * 100;
            mResult.OutRolls = tgs.OutRolls / totalTgs * 100;

            SetResult(0, mResult.TotalRolls);
            SetResult(1, mResult.InRolls);
            SetResult(2, mResult.OutRolls);
        }
    }
}
