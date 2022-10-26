using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;

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

        public Redirects() : base(eMeasurements.Redirects)
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
            TrigramStatsResult tgs = (TrigramStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TrigramStats];
            mResult.TotalRedirects = tgs.TotalRedirects;

            SetResult(0, mResult.TotalRedirects);
        }
    }
}
