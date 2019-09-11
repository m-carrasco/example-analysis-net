using Backend.Analyses;
using Backend.Model;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewAnalyses
{
    public enum ZeroAnalysisResult
    {
        BOTTOM,
        ZERO,
        NONZERO,
        TOP
    }

    /* public static void TestZero()
    {
        int x = 8;
        int y = x;
        int z = 0;

        while (y > -1)
        {
            x = x / y;
            y = y - 2;
            z = 5;
        }

        int final_x = x;
        int final_y = y;
        int final_z = z;
    }
    */
    public class ZeroAnalysis : ForwardDataFlowAnalysis<IDictionary<IVariable, ZeroAnalysisResult>>
    {
        public ZeroAnalysis(ControlFlowGraph cfg) : base(cfg) {}

        private Dictionary<IVariable, ZeroAnalysisResult>[] initialValues;

        protected override IDictionary<IVariable, ZeroAnalysisResult> Copy(IDictionary<IVariable, ZeroAnalysisResult> value)
        {
            var result = new Dictionary<IVariable, ZeroAnalysisResult>();

            foreach (var kv in value)
                result[kv.Key] = kv.Value;

            return result;
        }

        public override DataFlowAnalysisResult<IDictionary<IVariable, ZeroAnalysisResult>>[] Analyze()
        {
            this.ComputeInitialValues(this.cfg);

            var result = base.Analyze();

            this.initialValues = null;

            return result;
        }

        protected override bool Compare(IDictionary<IVariable, ZeroAnalysisResult> oldValue, IDictionary<IVariable, ZeroAnalysisResult> newValue)
        {
            if (oldValue.Keys.Count == newValue.Keys.Count)
            {
                foreach (IVariable oldK in oldValue.Keys)
                {
                    if (!newValue.ContainsKey(oldK))
                        return false;
                    else
                    {
                        if (newValue[oldK] != oldValue[oldK])
                            return false;
                    }
                }

                return true;
            }
            else
                return false;
        }

        protected override IDictionary<IVariable, ZeroAnalysisResult> Flow(CFGNode node, IDictionary<IVariable, ZeroAnalysisResult> input)
        {
            var res = new Dictionary<IVariable, ZeroAnalysisResult>();

            // we are computing transfer[n](X) = gen(n) U (X - kill(n))

            foreach (Instruction ins in node.Instructions)
            {
                if (ins is LoadInstruction loadInstruction)
                {
                    if (loadInstruction.Operand is Constant constant)
                    {
                        if (constant.Value is int asInt)
                            res[loadInstruction.Result] = asInt == 0 ? ZeroAnalysisResult.ZERO : ZeroAnalysisResult.NONZERO;
                        else
                            throw new NotImplementedException();
                    }
                    else if (loadInstruction.Operand is IVariable variable)
                    {
                        // we should be careful when dealing with a variable parameter
                        if (variable.IsParameter)
                            throw new NotImplementedException();
                        if (res.ContainsKey(variable))
                            res[loadInstruction.Result] = res[variable];
                        else if (input.ContainsKey(variable)) 
                            res[loadInstruction.Result] = input[variable];
                        else // this should not happen
                            throw new NotImplementedException();
                    }
                    else
                        throw new NotImplementedException();
                }
                else if (ins is BinaryInstruction binaryInstruction)
                {
                    ZeroAnalysisResult left = ZeroAnalysisResult.BOTTOM;
                    if (!res.TryGetValue(binaryInstruction.LeftOperand, out left))
                        input.TryGetValue(binaryInstruction.LeftOperand, out left);

                    ZeroAnalysisResult right = ZeroAnalysisResult.BOTTOM;
                    if (!res.TryGetValue(binaryInstruction.RightOperand, out right))
                        input.TryGetValue(binaryInstruction.RightOperand, out right);

                    if (binaryInstruction.Operation == BinaryOperation.Div)
                    {
                        ZeroAnalysisResult[,] divisionResult = new ZeroAnalysisResult[4, 4]
                        {

                            // BOTTOM _ with should never happen. (local) Variables are initialized before any use.
                            // what should happen with ZERO/TOP ?
                            // what should happen with NONZERO/ZERO and NONZERO/TOP?
                           //               BOTTOM                       ZERO                    NONZERO                 TOP
                           /* BOTTOM */ {ZeroAnalysisResult.BOTTOM, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                           /* ZERO */   {ZeroAnalysisResult.TOP,    ZeroAnalysisResult.ZERO, ZeroAnalysisResult.ZERO, ZeroAnalysisResult.TOP },
                           /* NONZERO */{ZeroAnalysisResult.TOP,    ZeroAnalysisResult.TOP, ZeroAnalysisResult.NONZERO, ZeroAnalysisResult.TOP },
                           /* TOP */    {ZeroAnalysisResult.TOP,    ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                        };

                        res[binaryInstruction.Result] = divisionResult[(int)left, (int)right];

                    }
                    else if (binaryInstruction.Operation == BinaryOperation.Add)
                    {
                        ZeroAnalysisResult[,] addResult = new ZeroAnalysisResult[4, 4]
                        {

                            // BOTTOM _ with should never happen. (local) Variables are initialized before any use.
                            // what should happen with ZERO/TOP ?
                            // what should happen with NONZERO/ZERO and NONZERO/TOP?
                           //               BOTTOM                       ZERO                    NONZERO                 TOP
                           /* BOTTOM */ {ZeroAnalysisResult.BOTTOM, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                           /* ZERO */   {ZeroAnalysisResult.TOP,    ZeroAnalysisResult.ZERO, ZeroAnalysisResult.NONZERO, ZeroAnalysisResult.TOP },
                           /* NONZERO */{ZeroAnalysisResult.TOP,    ZeroAnalysisResult.NONZERO, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                           /* TOP */    {ZeroAnalysisResult.TOP,    ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                        };

                        res[binaryInstruction.Result] = addResult[(int)left, (int)right];
                    }
                    else if (binaryInstruction.Operation == BinaryOperation.Sub)
                    {
                        ZeroAnalysisResult[,] subResult = new ZeroAnalysisResult[4, 4]
                        {

                            // BOTTOM _ with should never happen. (local) Variables are initialized before any use.
                            // what should happen with ZERO/TOP ?
                            // what should happen with NONZERO/ZERO and NONZERO/TOP?
                           //               BOTTOM                       ZERO                    NONZERO                 TOP
                           /* BOTTOM */ {ZeroAnalysisResult.BOTTOM, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                           /* ZERO */   {ZeroAnalysisResult.TOP,    ZeroAnalysisResult.ZERO, ZeroAnalysisResult.NONZERO, ZeroAnalysisResult.TOP },
                           /* NONZERO */{ZeroAnalysisResult.TOP,    ZeroAnalysisResult.NONZERO, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                           /* TOP */    {ZeroAnalysisResult.TOP,    ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP, ZeroAnalysisResult.TOP },
                        };

                        res[binaryInstruction.Result] = subResult[(int)left, (int)right];
                    } else
                        throw new NotImplementedException();
                }
                else if (ins is UnconditionalBranchInstruction unconditionalBranch)
                {

                }
                else if (ins is ConditionalBranchInstruction conditionalBranch)
                {

                }
                else if (ins is ReturnInstruction returnInstruction)
                {

                }
                else
                    throw new NotImplementedException();
            }

            foreach (var k in input.Keys.Except(res.Keys))
                res[k] = input[k];

            return res;
        }

        private void ComputeInitialValues(ControlFlowGraph cfg)
        {
            var r = new Dictionary<IVariable, ZeroAnalysisResult>[cfg.Nodes.Count];
            var variables = cfg.Nodes.SelectMany(n => n.Instructions).SelectMany(i => i.Variables).ToHashSet<IVariable>();

            // Initial values are computed on load instructions assigning constants
            foreach (var node in this.cfg.Nodes)
            {
                r[node.Id] = new Dictionary<IVariable, ZeroAnalysisResult>();

                // we set initial results based on constants loading
                foreach (LoadInstruction load in node.Instructions.OfType<LoadInstruction>())
                {
                    if (load.Operand is Constant constant)
                    {
                        if (constant.Value is int valAsInt)
                            r[node.Id][load.Result] = valAsInt == 0 ? ZeroAnalysisResult.ZERO : ZeroAnalysisResult.NONZERO;
                        else
                            throw new NotImplementedException();
                    }
                }

                foreach (var v in variables.Except(r[node.Id].Keys))
                    r[node.Id][v] = ZeroAnalysisResult.BOTTOM;
            }

            this.initialValues = r;
        }
        protected override IDictionary<IVariable, ZeroAnalysisResult> InitialValue(CFGNode node)
        {
            return this.initialValues[node.Id];
        }

        protected override IDictionary<IVariable, ZeroAnalysisResult> Join(IDictionary<IVariable, ZeroAnalysisResult> left, IDictionary<IVariable, ZeroAnalysisResult> right)
        {
            var r = new Dictionary<IVariable, ZeroAnalysisResult>();

            var keys = left.Keys.Union(right.Keys);

            foreach (var k in keys)
            {
                if (left.ContainsKey(k) && right.ContainsKey(k))
                {
                    var left_val = left[k];
                    var right_val = right[k];
                    r[k] = (ZeroAnalysisResult)Math.Max((int)left_val, (int)right_val);
                } else if (left.ContainsKey(k))
                {
                    r[k] = left[k];
                } else
                {
                    r[k] = right[k];
                }
            }

            return r;
        }
    }
}
