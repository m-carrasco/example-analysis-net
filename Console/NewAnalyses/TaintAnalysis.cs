using Backend.Analyses;
using Backend.Model;
using Console.Utils;
using Model;
using Model.ThreeAddressCode.Expressions;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Visitor;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAnalyses
{
    public enum TaintAnalysisStatus
    {
        NONE = 0,
        LOW = 1,
        HIGH = 2,
        TOP = 3
    };

    // for performance reasons we may want to change the implementation
    public interface ITaintAnalysisResult : IEquatable<ITaintAnalysisResult>, ICloneable
    {
        ISet<IVariable> Domain();
        TaintAnalysisStatus GetTaint(IVariable var);
        void SetTaint(IVariable var, TaintAnalysisStatus s);
        ITaintAnalysisResult Join(ITaintAnalysisResult input);
    }

    public class TaintAnalysisResult : ITaintAnalysisResult
    {

        public TaintAnalysisResult()
        {
            res = new Dictionary<IVariable, TaintAnalysisStatus>();
        }

        public TaintAnalysisStatus GetTaint(IVariable var)
        {
            return res[var];
        }

        public void SetTaint(IVariable var, TaintAnalysisStatus s)
        {
            res[var] = s;
        }

        public object Clone()
        {
            var result = new TaintAnalysisResult();
            foreach (var kv in res)
                result.SetTaint(kv.Key, this.GetTaint(kv.Key));

            return result;
        }

        public ITaintAnalysisResult Join(ITaintAnalysisResult input)
        {
            // we assume both have the same domain
            var thisDomain = this.Domain();

            var result = new TaintAnalysisResult();
            foreach (var k in thisDomain)
            {
                var l = this.GetTaint(k);
                var r = input.GetTaint(k);
                result.SetTaint(k, (TaintAnalysisStatus)Math.Max((int)l, (int)r));
            }

            return result;
        }

        public bool Equals(ITaintAnalysisResult other)
        {
            var thisDomain = this.Domain();

            if (thisDomain.SetEquals(other.Domain()))
            {
                foreach (var k in thisDomain)
                {
                    if (this.GetTaint(k) != other.GetTaint(k))
                        return false;
                }

                return true;
            }

            return false;
        }

        public ISet<IVariable> Domain()
        {
            return res.Keys.ToSet();
        }

        IDictionary<IVariable, TaintAnalysisStatus> res;
    }


    public class TaintAnalysis : ForwardDataFlowAnalysis<ITaintAnalysisResult>
    {
        class TaintVisitor : InstructionVisitor
        {
            private ITaintAnalysisResult input;
            public ITaintAnalysisResult Result;

            public TaintVisitor(ITaintAnalysisResult input)
            {
                this.input = input;
                this.Result = (ITaintAnalysisResult)input.Clone();
            }

            public override void Visit(LoadInstruction instruction) {

                var operand = instruction.Operand;
                var result = instruction.Result;
                if (operand is Constant constant)
                {
                    Result.SetTaint(result, TaintAnalysisStatus.NONE);
                } else if (operand is IVariable variable)
                {
                    Result.SetTaint(result, input.GetTaint(variable));
                } else
                    throw new NotImplementedException();
            }

            public override void Visit(MethodCallInstruction instruction) {

                var name = instruction.Method.Name;
                if (name.Equals("TaintHigh"))
                {
                    Result.SetTaint(instruction.Result, TaintAnalysisStatus.HIGH);
                    return;
                }
                else if (name.Equals("TaintLow"))
                {
                    if (instruction.Arguments.Any(v => input.GetTaint(v) > TaintAnalysisStatus.LOW))
                        Result.SetTaint(instruction.Result, TaintAnalysisStatus.HIGH);
                    else
                        Result.SetTaint(instruction.Result, TaintAnalysisStatus.LOW);
                    return;
                }
                else if (instruction.Method.ReturnType != PlatformTypes.Void)
                {
                    ControlFlowGraph cfg;
                    MethodBody methodBody = Transformations.ThreeAddressCode(instruction.Method.ResolvedMethod, out cfg);

                    var analysisParameters = new TaintAnalysisResult();
                    var variables = cfg.Nodes.SelectMany(n => n.Instructions).SelectMany(i => i.Variables);

                    foreach (var v in variables)
                        analysisParameters.SetTaint(v, TaintAnalysisStatus.NONE);

                    for (int i = 0; i < methodBody.Parameters.Count; i++)
                    {
                        IVariable parameter = methodBody.Parameters.ElementAt(i);
                        IVariable argument = instruction.Arguments.ElementAt(i);
                        analysisParameters.SetTaint(parameter, input.GetTaint(argument));
                    }

                    TaintAnalysis taintAnalysis = new TaintAnalysis(cfg, analysisParameters);
                    var result = taintAnalysis.Analyze();

                    var exitResult = result[cfg.Exit.Id].Output;

                    // que variable es el exit?
                    // esto puede fallar si hay mas de una, que es posible
                    var returnIns = cfg.Nodes.SelectMany(n => n.Instructions).OfType<ReturnInstruction>().First();

                    Result.SetTaint(instruction.Result, exitResult.GetTaint(returnIns.Variables.First()));
                }
            }
            public override void Visit(UnconditionalBranchInstruction instruction) { }
            public override void Visit(ConditionalBranchInstruction instruction) { }
            public override void Visit(BinaryInstruction instruction) {
                TaintAnalysisStatus r = (TaintAnalysisStatus)Math.Max((int)input.GetTaint(instruction.LeftOperand), (int)input.GetTaint(instruction.RightOperand));
                Result.SetTaint(instruction.Result, r);
            }
            public override void Visit(ReturnInstruction instruction) { }
            public override void Visit(NopInstruction instruction) { }


            // these are abstract classes, we override the implementations
            public override void Visit(DefinitionInstruction instruction) {}
            public override void Visit(Instruction instruction) {}
            public override void Visit(BranchInstruction instruction) {}
               
            // *************************************************************************************************************

            public override void Visit(UnaryInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(StoreInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(BreakpointInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(TryInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(FaultInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(FinallyInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(CatchInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(ConvertInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(ThrowInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(ExceptionalBranchInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(SwitchInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(SizeofInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(LoadTokenInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(IndirectMethodCallInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(CreateObjectInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(CopyMemoryInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(LocalAllocationInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(InitializeMemoryInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(InitializeObjectInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(CopyObjectInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(CreateArrayInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(PhiInstruction instruction) { throw new NotImplementedException(); }
        }

        IEnumerable<IInstruction> instructions;
        IEnumerable<IVariable> variables;
        ITaintAnalysisResult initialValue;
        ITaintAnalysisResult entryInitialValue;

        public IDictionary<Instruction, ITaintAnalysisResult> ResultForInstructions;
        public TaintAnalysis(ControlFlowGraph cfg, ITaintAnalysisResult entryInitialValue = null) : base(cfg)
        {
            instructions = cfg.Nodes.SelectMany(n => n.Instructions);
            variables = cfg.Nodes.SelectMany(n => n.Instructions.SelectMany(i => i.Variables));

            initialValue = new TaintAnalysisResult();
            foreach (var ins in instructions)
                foreach (var v in variables)
                    initialValue.SetTaint(v, TaintAnalysisStatus.NONE);

            this.entryInitialValue = entryInitialValue == null ? initialValue : entryInitialValue; 

            ResultForInstructions = new Dictionary<Instruction, ITaintAnalysisResult>();
        }
        protected override ITaintAnalysisResult Copy(ITaintAnalysisResult value)
        {
            return (ITaintAnalysisResult)value.Clone();
        }

        protected override bool Compare(ITaintAnalysisResult oldValue, ITaintAnalysisResult newValue)
        {
            return oldValue.Equals(newValue);
        }

        protected override ITaintAnalysisResult InitialValue(CFGNode node)
        {
            if (node.Kind == CFGNodeKind.Entry)
                return this.entryInitialValue;

            return initialValue;
        }

        protected override ITaintAnalysisResult Join(ITaintAnalysisResult left, ITaintAnalysisResult right)
        {
            return left.Join(right);
        }

        protected override ITaintAnalysisResult Flow(CFGNode node, ITaintAnalysisResult input)
        {
            if (node.Kind == CFGNodeKind.Exit)
            {
                return input;
            }

            foreach (Instruction instruction in node.Instructions.Cast<Instruction>())
            {
                var r = Flow(instruction, input);
                ResultForInstructions[instruction] = r;
                input = r;
            }

            return input;
        }
        
        private ITaintAnalysisResult Flow(Instruction instruction, ITaintAnalysisResult input)
        {
            TaintVisitor taintVisitor = new TaintVisitor(input);
            instruction.Accept(taintVisitor);
            return taintVisitor.Result;
        }
    }
}
