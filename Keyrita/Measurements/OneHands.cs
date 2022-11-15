using System;
using Keyrita.Analysis;
using Keyrita.Analysis.AnalysisUtil;
using Keyrita.Settings;

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
        public double OneHandsLeft { get; set; }
        public double OneHandsRight { get; set; }
    }

    public class OneHands : DynamicMeasurement
    {
        private OneHandsResult mResult;

        public OneHands(AnalysisGraph graph) : base(eMeasurements.OneHands, graph)
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
            double totalTgs = SettingState.MeasurementSettings.TrigramCoverage.Value;

            TrigramStatsResult tgs = (TrigramStatsResult)AnalysisGraph.ResolvedNodes[eInputNodes.TrigramStats];
            mResult.TotalOneHands = tgs.TotalOneHands / totalTgs * 100;
            mResult.OneHandsLeft = tgs.OneHandsLeft / totalTgs * 100;
            mResult.OneHandsRight = tgs.OneHandsRight / totalTgs * 100;

            SetResult(0, mResult.TotalOneHands);
            SetResult(1, mResult.OneHandsLeft);
            SetResult(2, mResult.OneHandsRight);
        }
    }
}
