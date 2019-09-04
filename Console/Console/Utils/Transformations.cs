using Backend;
using Backend.Analyses;
using Backend.Model;
using Backend.Transformations;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console.Utils
{
    class Transformations
    {
        // this function applies analysis-net analyses on the method defined in our assembly (methodDefinition)
        // the result is a typed stackless three address code representation of the orignal method definition body
        // you can 'out' the control flow graph because it can be reused for another analysis
        public static MethodBody ThreeAddressCode(IMethodDefinition methodDefinition, MetadataReaderHost host, out ControlFlowGraph cfg)
        {
            if (methodDefinition.IsAbstract || methodDefinition.IsExternal)
            {
                cfg = null;
                return null;
            }

            var disassembler = new Disassembler(host, methodDefinition, null);
            var methodBody = disassembler.Execute();

            var cfAnalysis = new ControlFlowAnalysis(methodBody);
            //var cfg = cfAnalysis.GenerateNormalControlFlow();
            cfg = cfAnalysis.GenerateExceptionalControlFlow();

            var splitter = new WebAnalysis(cfg, methodDefinition);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            var typeAnalysis = new TypeInferenceAnalysis(cfg, methodDefinition.Type);
            typeAnalysis.Analyze();

            methodBody.UpdateVariables();

            return methodBody;
            ////var dot = DOTSerializer.Serialize(cfg);
            //var dgml = DGMLSerializer.Serialize(cfg);
        }
    }
}
