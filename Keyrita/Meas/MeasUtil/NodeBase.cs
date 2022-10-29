using System;
using System.Collections.Generic;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Operations.OperationUtil
{
    /// <summary>
    /// The result of an operator.
    /// Each op can have many inputs, but only one output.
    /// The operator may NOT write to any of its inputs.
    /// </summary>
    public abstract class AnalysisResult 
    {
        public Enum ResultId { get; private set; }

        /// <summary>
        /// Standard construtor.
        /// </summary>
        /// <param name="resultId"></param>
        public AnalysisResult(Enum resultId)
        {
            this.ResultId = resultId;
        }
    }

    /// <summary>
    /// Class used by measurements. Each key needs to report how much it contributes to the total 
    /// score in order for generate to function properly.
    /// </summary>
    public abstract class PerKeyAnalysisResult : AnalysisResult
    {
        protected PerKeyAnalysisResult(Enum resultId) : base(resultId)
        {
        }

        public void Clear()
        {
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    PerKeyResult[i, j] = 0;
                }
            }
        }

        // Each key is given a total sfb score. Needed for finding the worst keys on the keyboard.
        public double[,] PerKeyResult { get; private set; } = new double[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
    }

    /// <summary>
    /// Contains the base functionality of an operation.
    /// The core idea is that generic operations with dependencies can be plugged into the system.
    /// Each time the analyzer runs, the system runs through the network of operations and produces a result.
    /// Each operation is Id'd by an enumeration token. The network can reuse the result of operations for other
    /// operations down the road. A fully generic AnalysisResult class is used to communicate the results 
    /// to the operators. Only when an operation has completed can the dependent ops do their work.
    /// </summary>
    public abstract class GraphNode
    {
        /// <summary>
        /// To inform the gui if an operation has changed its value.
        /// </summary>
        public ChangeNotification ValueChangedNotifications = new ChangeNotification();

        /// <summary>
        /// The Id for this operator.
        /// </summary>
        public Enum NodeId { get; private set; }

        /// <summary>
        /// The list of operations which must be complete in order to compute this op.
        /// </summary>
        public IList<Enum> Inputs { get; private set; } = new List<Enum>();

        /// <summary>
        /// Standard constructor.
        /// Input operations should be added in the constructor.
        /// </summary>
        public GraphNode(Enum id)
        {
            NodeId = id;
        }

        public void PerformComputation()
        {
            Compute();
            ValueChangedNotifications.NotifyGui(this);
        }

        /// <summary>
        /// Abstract method which should be used to compute the result of the operation.
        /// </summary>
        protected abstract void Compute();

        public virtual void ConnectInputs()
        {
            foreach(Enum op in Inputs)
            {
                AnalysisGraphSystem.InstallNode(op);
            }
        }

        /// <summary>
        /// Adds an input operator to the network.
        /// </summary>
        /// <param name="inputNode"></param>
        public void AddInputNode(Enum inputNode)
        {
            LogUtils.Assert(!Inputs.Contains(inputNode), $"This node already depends on {inputNode}");
            Inputs.Add(inputNode);
        }

        /// <summary>
        /// Operators must have a way to get the abstract analysis result.
        /// </summary>
        /// <returns></returns>
        public abstract AnalysisResult GetResult();
    }
}
