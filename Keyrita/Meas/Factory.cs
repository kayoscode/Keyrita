using Keyrita.Meas;
using Keyrita.Measurements;
using Keyrita.Operations.OperationUtil;
using Keyrita.Util;
using System;

namespace Keyrita.Operations
{
    public abstract class NodeFactory
    {
        public NodeFactory()
        {
        }

        public abstract GraphNode CreateOp(Enum op);
    }

    public class MeasFactory : NodeFactory
    {
        public MeasFactory() { }

        public GraphNode CreateMeasurement(eMeasurements meas)
        {
            switch (meas)
            {
                case eMeasurements.SameFingerBigram:
                    return new FindSFBs();
                case eMeasurements.Rolls:
                    return new Rolls();
                case eMeasurements.Alternations:
                    return new Alts();
                case eMeasurements.Redirects:
                    return new Redirects();
                case eMeasurements.OneHands:
                    return new OneHands();
                case eMeasurements.FingerBalance:
                    return new FingerBalance();
                default:
                    return null;
            }
        }

        public override GraphNode CreateOp(Enum op)
        {
            LogUtils.Assert(op is eMeasurements, "Invalid measurement op");
            return CreateMeasurement((eMeasurements)op);
        }
    }

    public class InputFactory : NodeFactory
    {
        public InputFactory() { }

        public GraphNode CreateDependentOp(eInputNodes depOp)
        {
            switch (depOp)
            {
                case eInputNodes.CharacterSetAsList:
                    return new CharacterSetAsList();
                case eInputNodes.KeyToFingerAsInt:
                    return new KeyToFingerAsInt();
                case eInputNodes.TransfomedKbState:
                    return new TransformedKbState();
                case eInputNodes.BigramClassification:
                    return new BigramClassification();
                case eInputNodes.TransformedCharacterToFingerAsInt:
                    return new TransformedCharacterToFingerAsInt();
                case eInputNodes.TransformedCharacterToKey:
                    return new TransformedCharacterToKey();
                case eInputNodes.TrigramStats:
                    return new TrigramStats();
                default:
                    return null;
            }
        }

        public override GraphNode CreateOp(Enum op)
        {
            LogUtils.Assert(op is eInputNodes);
            return CreateDependentOp((eInputNodes)op);
        }
    }
}
