using System;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;

namespace Keyrita.Measurements
{
    public class LayoutScoreResult : AnalysisResult
    {
        public LayoutScoreResult(Enum resultId) : base(resultId)
        {
        }

        public double TotalScore;
    }

    public class LayoutScore : DynamicMeasurement
    {
        protected double KEY_LAG_WEIGHT = 8;
        protected double ROLES_WEIGHT = -20;
        protected double REDIRECTS_WEIGHT = 150;
        protected double ONE_HANDS_WEIGHT = -20; 
        protected double ALTERNATIONS_WEIGHT = -20; 

        private LayoutScoreResult mResult;
        public LayoutScore() : base(eMeasurements.LayoutScore)
        {
            AddInputNode(eInputNodes.KeyLag);
            AddInputNode(eInputNodes.TrigramStats);
        }

        public override bool RespondsToGenerateSwapKeysEvent => true;
        public override void SwapBack()
        {
            ComputeResult();
        }

        public override void SwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            ComputeResult();
        }

        public override AnalysisResult GetResult()
        {
            return mResult;
        }

        protected KeyLagResult mKeyLag;
        protected TrigramStatsResult mTgStats;

        protected override void Compute()
        {
            mResult = new LayoutScoreResult(this.NodeId);
            mKeyLag = (KeyLagResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyLag];
            mTgStats = (TrigramStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TrigramStats];

            ComputeResult();

            SetResult(0, mResult.TotalScore);
        }

        protected void ComputeResult()
        {
            double tgTotal = SettingState.MeasurementSettings.CharFrequencyData.TrigramHitCount;

            mResult.TotalScore = mKeyLag.TotalResult * KEY_LAG_WEIGHT;

            mResult.TotalScore += (mTgStats.TotalRolls / tgTotal) * ROLES_WEIGHT;
            mResult.TotalScore += (mTgStats.TotalRedirects / tgTotal) * REDIRECTS_WEIGHT;
            mResult.TotalScore += (mTgStats.TotalOneHands / tgTotal) * ONE_HANDS_WEIGHT;
            mResult.TotalScore += (mTgStats.TotalAlternations / tgTotal) * ALTERNATIONS_WEIGHT;
        }
    }
}
