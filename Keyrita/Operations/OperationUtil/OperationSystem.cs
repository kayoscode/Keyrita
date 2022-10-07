using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Installs an op into this network.
        /// </summary>
        /// <param name="op"></param>
        public static void InstallOp(OperationBase op)
        {
            if (!InstalledOps.ContainsKey(op.Op))
            {
                InstalledOps[op.Op] = op;
            }
        }

        /// <summary>
        /// Removes an operation from the network.
        /// </summary>
        /// <param name="op"></param>
        public static void UninstallOp(OperationBase op)
        {
            if (InstalledOps.ContainsKey(op.Op))
            {
                InstalledOps.Remove(op.Op);
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
                LTrace.Assert(op.Value != null);
                LTrace.Assert(op.Value.Op == op.Key);
                ResolveOp(op.Value);
            }
        }

        /// <summary>
        /// Resolves a single operation and ensures the dependents are all resolved. Othewise it resolves them.
        /// </summary>
        /// <param name="op"></param>
        public static void ResolveOp(OperationBase op)
        {
            if (ResolvedOps.ContainsKey(op.OutputId))
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
            ResolvedOps[op.OutputId] = result;
        }
    }
}
