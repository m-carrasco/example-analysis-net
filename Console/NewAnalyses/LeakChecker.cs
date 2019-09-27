using Backend.Model;
using Model;
using Model.ThreeAddressCode.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewAnalyses
{
    public class LeakChecker
    {
        private readonly ControlFlowGraph cfg;

        public LeakChecker(ControlFlowGraph cfg)
        {
            this.cfg = cfg;
        }

        public IList<IInstruction> Analyze()
        {
            IList<IInstruction> result = new List<IInstruction>();
            TaintAnalysis taintAnalysis = new TaintAnalysis(cfg);

            var taintResultBB = taintAnalysis.Analyze();

            var taintResultInstructions = taintAnalysis.ResultForInstructions;

            foreach (CFGNode node in cfg.Nodes)
            {
                for (int i =0; i < node.Instructions.Count; i++)
                {
                    MethodCallInstruction call = node.Instructions[i] as MethodCallInstruction;

                    if (call == null)
                        continue;

                    // skip hardcoded
                    if (call.Method.Name.Equals("TaintHigh") ||
                        call.Method.Name.Equals("TaintLow"))
                        continue;

                    // find sucessors

                    if (i > 0)
                    {
                        // it is in same bb
                        var succ = node.Instructions[i - 1];
                        var taintStatus = taintResultInstructions[succ as Instruction];

                        if (call.Arguments.Any(a => taintStatus.GetTaint(a) >= TaintAnalysisStatus.LOW))
                            result.Add(call);
                    }
                    else
                    {
                        // check bb predecesors
                        var predecesorsIns = node.Predecessors.Select(n => n.Instructions.Last());
                        var predecesorsRes = predecesorsIns.Select(ins => taintResultInstructions[ins as Instruction]);

                        if (predecesorsRes.Any(r => call.Arguments.Any(a => r.GetTaint(a) >= TaintAnalysisStatus.LOW)))
                            result.Add(call);
                    }
                }
            }
            
            return result;
        }
    }
}
