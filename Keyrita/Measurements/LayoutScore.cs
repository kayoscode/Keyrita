using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;

namespace Keyrita.Measurements
{
    public class LayoutScoreResult : AnalysisResult
    {
        public LayoutScoreResult(Enum resultId) : base(resultId)
        {
        }
    }

    public class LayoutScore : GraphNode
    {
        private LayoutScoreResult mResult;
        public LayoutScore() : base(eMeasurements.LayoutScore)
        {
        }

        public override bool RespondsToGenerateSwapKeysEvent => false;

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected override void Compute()
        {
            mResult = new LayoutScoreResult(this.NodeId);
        }
    }
}
