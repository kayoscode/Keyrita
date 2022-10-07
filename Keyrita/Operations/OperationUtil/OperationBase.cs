using System;
using System.Collections.Generic;
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
    /// Contains the base functionality of an operation.
    /// The core idea is that generic operations with dependencies can be plugged into the system.
    /// Each time the analyzer runs, the system runs through the network of operations and produces a result.
    /// Each operation is Id'd by an enumeration token. The network can reuse the result of operations for other
    /// operations down the road. A fully generic AnalysisResult class is used to communicate the results 
    /// to the operators. Only when an operation has completed can the dependent ops do their work.
    /// </summary>
    public abstract class OperationBase
    {
        /// <summary>
        /// The Id for this operator.
        /// </summary>
        public Enum Op { get; private set; }

        /// <summary>
        /// The identifier for the output result.
        /// </summary>
        public Enum OutputId { get; private set; }

        /// <summary>
        /// The list of operations which must be complete in order to compute this op.
        /// </summary>
        public IList<Enum> InputOps { get; private set; } = new List<Enum>();

        /// <summary>
        /// Standard constructor.
        /// Input operations should be added in the constructor.
        /// </summary>
        public OperationBase(Enum id)
        {
            Op = id;
        }

        /// <summary>
        /// Abstract method which should be used to compute the result of the operation.
        /// </summary>
        public abstract void Compute();

        /// <summary>
        /// Adds an input operator to the network.
        /// </summary>
        /// <param name="op"></param>
        public void AddInputOp(Enum op)
        {
            LTrace.Assert(!InputOps.Contains(op), "Operation already depends on this op");
            InputOps.Add(op);
        }

        /// <summary>
        /// Operators must have a way to get the abstract analysis result.
        /// </summary>
        /// <returns></returns>
        public abstract AnalysisResult GetResult();

        /// <summary>
        /// Asserts that the result of this operation will go into the specified signal.
        /// </summary>
        /// <param name="signal"></param>
        public void SetOutputId(Enum signal)
        {
            OutputId = signal;
        }
    }
}
