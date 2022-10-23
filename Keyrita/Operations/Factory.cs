using Keyrita.Measurements;
using Keyrita.Operations.OperationUtil;
using Keyrita.Util;
using System;

namespace Keyrita.Operations
{
    public abstract class OpFactory
    {
        public OpFactory()
        {
        }

        public abstract OperationBase CreateOp(Enum op);
    }

    public class MeasOpFactory : OpFactory
    {
        public MeasOpFactory() { }

        public OperationBase CreateMeasurement(eMeasurements meas)
        {
            switch (meas)
            {
                case eMeasurements.SameFingerBigram:
                    return new FindSFBs(meas);
                case eMeasurements.SameFingerSkipgrams:
                    return new FindSFBs(meas);
                default:
                    return null;
            }
        }

        public override OperationBase CreateOp(Enum op)
        {
            LTrace.Assert(op is eMeasurements, "Invalid measurement op");
            return CreateMeasurement((eMeasurements)op);
        }
    }

    public class DependentOpFactory : OpFactory
    {
        public DependentOpFactory() { }

        public OperationBase CreateDependentOp(eDependentOps depOp)
        {
            switch (depOp)
            {
                case eDependentOps.CharacterSetAsList:
                    return new CharacterSetAsList(depOp);
                case eDependentOps.KeyToFingerAsInt:
                    return new KeyToFingerAsInt(depOp);
                case eDependentOps.TransfomedKbState:
                    return new TransformedKbState(depOp);
                default:
                    return null;
            }
        }

        public override OperationBase CreateOp(Enum op)
        {
            LTrace.Assert(op is eDependentOps);
            return CreateDependentOp((eDependentOps)op);
        }
    }
}
