using Keyrita.Meas;
using Keyrita.Measurements;
using Keyrita.Analysis.AnalysisUtil;
using Keyrita.Util;
using System;

namespace Keyrita.Analysis
{
    public abstract class NodeFactory
    {
        public NodeFactory()
        {
        }

        public abstract GraphNode CreateOp(Enum op, AnalysisGraph graph);
    }

    public class MeasFactory : NodeFactory
    {
        public MeasFactory() { }

        public GraphNode CreateMeasurement(eMeasurements meas, AnalysisGraph graph)
        {
            switch (meas)
            {
                case eMeasurements.SameFingerBigram:
                    return new Sfbs(graph);
                case eMeasurements.SameFingerSkipgrams:
                    return new Sfss(graph);
                case eMeasurements.Rolls:
                    return new Rolls(graph);
                case eMeasurements.Alternations:
                    return new Alts(graph);
                case eMeasurements.Redirects:
                    return new Redirects(graph);
                case eMeasurements.OneHands:
                    return new OneHands(graph);
                case eMeasurements.FingerBalance:
                    return new FingerBalance(graph);
                case eMeasurements.HomeRowUsage:
                    return new HomeUsage(graph);
                case eMeasurements.FingerLag:
                    return new FingerLag(graph);
                case eMeasurements.Scissors:
                    return new UserFacingScissors(graph);
                case eMeasurements.LayoutScore:
                    return new LayoutScore(graph);
                default:
                    return null;
            }
        }

        public override GraphNode CreateOp(Enum op, AnalysisGraph graph)
        {
            LogUtils.Assert(op is eMeasurements, "Invalid measurement op");
            return CreateMeasurement((eMeasurements)op, graph);
        }
    }

    public class InputFactory : NodeFactory
    {
        public InputFactory() { }

        public GraphNode CreateDependentOp(eInputNodes depOp, AnalysisGraph graph)
        {
            switch (depOp)
            {
                case eInputNodes.CharacterSetAsList:
                    return new CharacterSetAsList(graph);
                case eInputNodes.KeyToFingerAsInt:
                    return new KeyToFingerAsInt(graph);
                case eInputNodes.TransfomedKbState:
                    return new TransformedKbState(graph);
                case eInputNodes.TransformedCharacterToFingerAsInt:
                    return new TransformedCharacterToFingerAsInt(graph);
                case eInputNodes.TransformedCharacterToKey:
                    return new TransformedCharacterToKey(graph);
                case eInputNodes.TrigramStats:
                    return new TrigramStats(graph);
                case eInputNodes.FingerAsIntToHomePosition:
                    return new FingerAsIntToHomePosition(graph);
                case eInputNodes.TwoFingerStats:
                    return new TwoFingerStats(graph);
                case eInputNodes.KeyLag:
                    return new KeyLag(graph);
                case eInputNodes.SameFingerMap:
                    return new SameFingerMap(graph);
                case eInputNodes.ScissorsIntermediate:
                    return new ScissorsIntermediate(graph);
                default:
                    return null;
            }
        }

        public override GraphNode CreateOp(Enum op, AnalysisGraph graph)
        {
            LogUtils.Assert(op is eInputNodes);
            return CreateDependentOp((eInputNodes)op, graph);
        }
    }
}
