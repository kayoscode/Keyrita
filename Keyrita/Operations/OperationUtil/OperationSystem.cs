using System;
using System.Collections.Generic;
using Keyrita.Measurements;
using Keyrita.Util;

namespace Keyrita.Operations.OperationUtil
{
    /// <summary>
    /// Class responsible for creating and maintaining operators.
    /// </summary>
    public static class OperationSystem
    {
        private static IDictionary<Enum, OperationBase> InstalledOps = new Dictionary<Enum, OperationBase>();
        public static IDictionary<Enum, AnalysisResult> ResolvedOps = new Dictionary<Enum, AnalysisResult>();

        private static IReadOnlyDictionary<Type, OpFactory> mOpFactories = new Dictionary<Type, OpFactory>()
        {
            { typeof(eMeasurements), new MeasOpFactory() },
            { typeof(eDependentOps), new DependentOpFactory() },
        };

        /// <summary>
        /// Installs an op into this network.
        /// </summary>
        /// <param name="op"></param>
        public static void InstallOp(Enum op)
        {
            LTrace.Assert(mOpFactories.ContainsKey(op.GetType()), "Invalid op type");

            if (!InstalledOps.ContainsKey(op))
            {
                InstalledOps[op] = mOpFactories[op.GetType()].CreateOp(op);
                LTrace.Assert(InstalledOps[op] != null, "Unimplemented operation");
                InstalledOps[op].ConnectInputs();
            }
        }

        /// <summary>
        /// Removes an operation from the network.
        /// </summary>
        /// <param name="op"></param>
        public static void UninstallOp(Enum op)
        {
            LTrace.Assert(mOpFactories.ContainsKey(op.GetType()), "Invalid op type");

            if (InstalledOps.ContainsKey(op))
            {
                InstalledOps.Remove(op);
            }
            else
            {
                LTrace.Assert(false, "Attempted to remove an op that wasn't installed");
            }
        }

        /// <summary>
        /// Computes the results of all ops.
        /// </summary>
        public static void ResolveOps()
        {
            ResolvedOps.Clear();

            // Go through each operation, and make sure their dependents have been resolved. If so,
            foreach(var op in InstalledOps)
            {
                LTrace.Assert(op.Value != null, "Sanity check failed.");
                LTrace.Assert(op.Value.Op.Equals(op.Key), "Sanity check failed.");
                ResolveOp(op.Value);
            }
        }

        /// <summary>
        /// Resolves a single operation and ensures the dependents are all resolved. Othewise it resolves them.
        /// </summary>
        /// <param name="op"></param>
        public static void ResolveOp(OperationBase op)
        {
            if (ResolvedOps.ContainsKey(op.Op))
            {
                return;
            }

            foreach (Enum dependentOp in op.InputOps)
            {
                LTrace.Assert(InstalledOps.ContainsKey(dependentOp), "All dependent ops should be in the network.");
                ResolveOp(InstalledOps[dependentOp]);
            }

            op.Compute();
            var result = op.GetResult();

            LTrace.Assert(result != null);
            ResolvedOps[op.Op] = result;
        }
    }
}
