using System;
using System.Collections.Generic;
using Keyrita.Measurements;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Analysis.AnalysisUtil
{
    /// <summary>
    /// Holds all nodes used by a graph.
    /// </summary>
    public class AnalysisGraph
    {
        private static IReadOnlyDictionary<Type, NodeFactory> GraphNodeFactory = new Dictionary<Type, NodeFactory>()
        {
            { typeof(eMeasurements), new MeasFactory() },
            { typeof(eInputNodes), new InputFactory() },
        };

        /// <summary>
        /// The currently installed list of nodes.
        /// </summary>
        private IDictionary<Enum, GraphNode> ActiveNodes = new Dictionary<Enum, GraphNode>();
        
        /// <summary>
        /// The resolved nodes as a dictionary.
        /// </summary>
        public IDictionary<Enum, AnalysisResult> ResolvedNodes = new Dictionary<Enum, AnalysisResult>();

        /// <summary>
        /// The nodes affected by key swapping.
        /// </summary>
        private List<GraphNode> GenerateSwapKeysNodes = new List<GraphNode>();

        /// <summary>
        /// Sets up the generate algorithm to quickly swap keys during its lifetime.
        /// Creates the order such that it guarantees by the time a node runs, all its dependents have run.
        /// </summary>
        public void PreprocessSwapKeysResolveOrder()
        {
            GenerateSwapKeysNodes.Clear();
            Dictionary<Enum, GraphNode> insertedNodes = new Dictionary<Enum, GraphNode>();

            foreach(var node in ActiveNodes.Values)
            {
                PreprocessSwapKeysResolveOrderNode(insertedNodes, node);
            }
        }

        private void PreprocessSwapKeysResolveOrderNode(Dictionary<Enum, GraphNode> insertedNodes, GraphNode node)
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
        public void GenerateSignalSwapKeys(int k1i, int k1j, int k2i, int k2j)
        {
            // Instead of completely re analyzing, trigger swap keys event so each measurement can just respond to deltas.
            // This ensures we don't have to clear previous data, reallocate, nor re analyze the entire system.

            // Go through each node and make sure its dependents have been resolved. (swapped keys)
            for(int i = 0; i < GenerateSwapKeysNodes.Count; i++)
            {
                GenerateSwapKeysNodes[i].SwapKeys(k1i, k1j, k2i, k2j);
            }
        }

        /// <summary>
        /// Swap back to the previou state, each operator should have implemented the ability to do this.
        /// This shouldn't be called if Swap has not already been called.
        /// Each node needs to understand how to swap back, but they don't need to know how to swap back multiple times.
        /// Therefore this should only be called once per swap signal.
        /// </summary>
        public void GenerateSignalSwapBack()
        {
            for(int i = 0; i < GenerateSwapKeysNodes.Count; i++)
            {
                GenerateSwapKeysNodes[i].SwapBack();
            }
        }

        /// <summary>
        /// Returns a specific node by id.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public GraphNode GetInstalledNode(Enum node)
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
        public void InstallNode(Enum node)
        {
            LogUtils.Assert(GraphNodeFactory.ContainsKey(node.GetType()), "Invalid node type");

            if (!ActiveNodes.ContainsKey(node))
            {
                ActiveNodes[node] = GraphNodeFactory[node.GetType()].CreateOp(node, this);

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
        public void RemoveNode(Enum node)
        {
            LogUtils.Assert(GraphNodeFactory.ContainsKey(node.GetType()), "Invalid node type");

            if (ActiveNodes.ContainsKey(node))
            {
                //ActiveNodes.Remove(node);
            }
        }

        /// <summary>
        /// Whether or not the analyzer is in a position to get a valid result.
        /// </summary>
        public bool CanAnalyze =>
                SettingState.MeasurementSettings.CharFrequencyData.HasValue &&
                       SettingState.KeyboardSettings.KeyboardValid.Value.Equals(eOnOff.On) &&
                       SettingState.MeasurementSettings.AnalysisEnabled.Value.Equals(eOnOff.On);

        /// <summary>
        /// Computes the results of all ops.
        /// </summary>
        public void ResolveGraph()
        {
            if (CanAnalyze)
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
        }

        /// <summary>
        /// Resolves a single operation and ensures the dependents are all resolved. Othewise it resolves them.
        /// </summary>
        /// <param name="node"></param>
        public void ResolveNode(GraphNode node)
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

    /// <summary>
    /// Class responsible for creating and maintaining the analysis graph.
    /// Threaded support.
    /// </summary>
    public static class AnalysisGraphSystem
    {
        public static AnalysisGraph MainAnalysisGraph { get; private set; }

        static AnalysisGraphSystem()
        {
            MainAnalysisGraph = new AnalysisGraph();
        }

        /// <summary>
        /// Creates a new analysis graph completely isolated from any of the others.
        /// Returns an id to that graph, but thread id 0 is the main graph.
        /// </summary>
        /// <returns></returns>
        public static AnalysisGraph CreateNewGraph()
        {
            return new AnalysisGraph();
        }
    }
}
