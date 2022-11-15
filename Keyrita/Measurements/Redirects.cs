using System;
using Keyrita.Analysis;
using Keyrita.Analysis.AnalysisUtil;
using Keyrita.Settings;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Computes the rolls found in the keyboard layout.
    /// </summary>
    public class RedirectsResult : AnalysisResult
    {
        public RedirectsResult(Enum resultId) 
            : base(resultId)
        {
        }

        public double TotalRedirects { get; set; }
    }

    public class Redirects : DynamicMeasurement
    {
        private RedirectsResult mResult;

        public Redirects(AnalysisGraph graph) : base(eMeasurements.Redirects, graph)
        {
            mResult = new RedirectsResult(NodeId);
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
            mResult.TotalRedirects = tgs.TotalRedirects / totalTgs * 100;

            SetResult(0, mResult.TotalRedirects);
        }
    }
}
