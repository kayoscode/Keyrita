using System;
using System.Collections.Generic;
using Keyrita.Measurements;
using Keyrita.Util;

namespace Keyrita.Operations.OperationUtil
{
    /// <summary>
    /// Class responsible for creating and maintaining operators.
    /// </summary>
    public static class AnalysisGraphSystem
    {
        private static IDictionary<Enum, GraphNode> ActiveNodes = new Dictionary<Enum, GraphNode>();
        public static IDictionary<Enum, AnalysisResult> ResolvedNodes = new Dictionary<Enum, AnalysisResult>();

        private static IReadOnlyDictionary<Type, NodeFactory> GraphNodeFactory = new Dictionary<Type, NodeFactory>()
        {
            { typeof(eMeasurements), new MeasFactory() },
            { typeof(eInputNodes), new InputFactory() },
        };

        public static GraphNode GetInstalledOperation(Enum op)
        {
            if(op != null && ActiveNodes.TryGetValue(op, out GraphNode output))
            {
                return output;
            }

            return null;
        }

        /// <summary>
        /// Installs an op into this network.
        /// </summary>
        /// <param name="node"></param>
        public static void InstallNode(Enum node)
        {
            LogUtils.Assert(GraphNodeFactory.ContainsKey(node.GetType()), "Invalid node type");

            if (!ActiveNodes.ContainsKey(node))
            {
                ActiveNodes[node] = GraphNodeFactory[node.GetType()].CreateOp(node);
                LogUtils.Assert(ActiveNodes[node] != null, "Unimplemented node type.");

                if(ActiveNodes[node] != null)
                {
                    ActiveNodes[node].ConnectInputs();
                }
            }
        }

        /// <summary>
        /// Removes an operation from the network.
        /// </summary>
        /// <param name="op"></param>
        public static void RemoveNode(Enum op)
        {
            LogUtils.Assert(GraphNodeFactory.ContainsKey(op.GetType()), "Invalid op type");

            if (ActiveNodes.ContainsKey(op))
            {
                ActiveNodes.Remove(op);
            }
        }

        /// <summary>
        /// Computes the results of all ops.
        /// </summary>
        public static void ResolveGraph()
        {
            ResolvedNodes.Clear();

            // Go through each operation, and make sure their dependents have been resolved. If so,
            foreach(var op in ActiveNodes)
            {
                LogUtils.Assert(op.Value != null, "Sanity check failed.");
                LogUtils.Assert(op.Value.NodeId.Equals(op.Key), "Sanity check failed.");
                ResolveNode(op.Value);
            }
        }

        /// <summary>
        /// Resolves a single operation and ensures the dependents are all resolved. Othewise it resolves them.
        /// </summary>
        /// <param name="op"></param>
        public static void ResolveNode(GraphNode op)
        {
            if (ResolvedNodes.ContainsKey(op.NodeId))
            {
                return;
            }

            foreach (Enum depNode in op.Inputs)
            {
                LogUtils.Assert(ActiveNodes.ContainsKey(depNode), "All dependent ops should be in the network.");
                ResolveNode(ActiveNodes[depNode]);
            }

            op.PerformOp();
            var result = op.GetResult();

            LogUtils.Assert(result != null);
            ResolvedNodes[op.NodeId] = result;
        }
    }
}
