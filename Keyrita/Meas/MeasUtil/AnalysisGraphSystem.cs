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

        private static List<GraphNode> GenerateSwapKeysNodes = new List<GraphNode>();

        private static IReadOnlyDictionary<Type, NodeFactory> GraphNodeFactory = new Dictionary<Type, NodeFactory>()
        {
            { typeof(eMeasurements), new MeasFactory() },
            { typeof(eInputNodes), new InputFactory() },
        };

        /// <summary>
        /// Sets up the generate algorithm to quickly swap keys during its lifetime.
        /// Creates the order such that it guarantees by the time an operator runs, all its dependents have run.
        /// </summary>
        public static void PreprocessSwapKeysResolveOrder()
        {
            GenerateSwapKeysNodes.Clear();
            Dictionary<Enum, GraphNode> insertedNodes = new Dictionary<Enum, GraphNode>();

            foreach(var node in ActiveNodes.Values)
            {
                PreprocessSwapKeysResolveOrderNode(insertedNodes, node);
            }
        }

        private static void PreprocessSwapKeysResolveOrderNode(Dictionary<Enum, GraphNode> insertedNodes, GraphNode node)
        {
            if (!node.RespondsToGenerateSwapKeysEvent)
            {
                return;
            }
            if (insertedNodes.ContainsKey(node.NodeId))
            {
                return;
            }

            foreach (Enum depNode in node.Inputs)
            {
                LogUtils.Assert(ActiveNodes.ContainsKey(depNode), "All dependent nodes should be in the network.");
                PreprocessSwapKeysResolveOrderNode(insertedNodes, ActiveNodes[depNode]);
            }

            insertedNodes.Add(node.NodeId, node);
            GenerateSwapKeysNodes.Add(node);
        }

        /// <summary>
        /// If we get here, we are assuming the resolved node set is already filled. And a single analysis cycle has already taken place.
        /// </summary>
        /// <param name="k1i"></param>
        /// <param name="k1j"></param>
        /// <param name="k2i"></param>
        /// <param name="k2j"></param>
        public static void GenerateSignalSwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // Instead of completely re analyzing, trigger swap keys event so each measurement can just respond to deltas.
            // This ensures we don't have to clear previous data, reallocate, nor re analyze the entire system.

            // Go through each operator and make sure its dependents have been resolved. (swapped keys)
            for(int i = 0; i < GenerateSwapKeysNodes.Count; i++)
            {
                GenerateSwapKeysNodes[i].SwapKeys(k1i, k1j, k2i, k2j);
            }
        }

        public static GraphNode GetInstalledOperation(Enum node)
        {
            if(node != null && ActiveNodes.TryGetValue(node, out GraphNode output))
            {
                return output;
            }

            return null;
        }

        /// <summary>
        /// Installs a node into this network.
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
        /// Removes a node from the network.
        /// TODO:
        /// </summary>
        /// <param name="op"></param>
        public static void RemoveNode(Enum node)
        {
            LogUtils.Assert(GraphNodeFactory.ContainsKey(node.GetType()), "Invalid node type");

            if (ActiveNodes.ContainsKey(node))
            {
                //ActiveNodes.Remove(node);
            }
        }

        /// <summary>
        /// Computes the results of all ops.
        /// </summary>
        public static void ResolveGraph()
        {
            ResolvedNodes.Clear();

            // Go through each operation, and make sure their dependents have been resolved. If so,
            foreach(var node in ActiveNodes)
            {
                LogUtils.Assert(node.Value != null, "Sanity check failed.");
                LogUtils.Assert(node.Value.NodeId.Equals(node.Key), "Sanity check failed.");
                ResolveNode(node.Value);
            }
        }

        /// <summary>
        /// Resolves a single operation and ensures the dependents are all resolved. Othewise it resolves them.
        /// </summary>
        /// <param name="node"></param>
        public static void ResolveNode(GraphNode node)
        {
            if (ResolvedNodes.ContainsKey(node.NodeId))
            {
                return;
            }

            foreach (Enum depNode in node.Inputs)
            {
                LogUtils.Assert(ActiveNodes.ContainsKey(depNode), "All dependent nodes should be in the network.");
                ResolveNode(ActiveNodes[depNode]);
            }

            node.PerformComputation();
            var result = node.GetResult();

            LogUtils.Assert(result != null);
            ResolvedNodes[node.NodeId] = result;
        }
    }
}
