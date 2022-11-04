using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Computes the rolls found in the keyboard layout.
    /// </summary>
    public class AltsResult : AnalysisResult
    {
        public AltsResult(Enum resultId) 
            : base(resultId)
        {
        }

        public double TotalAlternations { get; set; }
    }

    public class Alts : DynamicMeasurement
    {
        private AltsResult mResult;

        public Alts() : base(eMeasurements.Alternations)
        {
            mResult = new AltsResult(NodeId);
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
            long totalTgs = tgSet.TrigramCoverage;

            mResult.TotalAlternations = tgs.TotalAlternations / totalTgs * 100;

            SetResult(0, mResult.TotalAlternations);
        }
    }
}
