using Backend.Analyses;
using Backend.Model;
using Backend.Transformations;
using Model.Types;

namespace Console.Utils
{
    public class Transformations
    {
        // this function applies analysis-net analyses on the method defined in our assembly (methodDefinition)
        // the result is a typed stackless three address code representation of the orignal method definition body
        // you can 'out' the control flow graph because it can be reused for another analysis
        public static MethodBody ThreeAddressCode(MethodDefinition methodDefinition, out ControlFlowGraph cfg)
        {
            if (methodDefinition.IsAbstract || methodDefinition.IsExternal)
            {
                cfg = null;
                return null;
            }

            var disassembler = new Disassembler(methodDefinition);
            var methodBody = disassembler.Execute();

            var cfAnalysis = new ControlFlowAnalysis(methodBody);
            //var cfg = cfAnalysis.GenerateNormalControlFlow();
            cfg = cfAnalysis.GenerateExceptionalControlFlow();

            var splitter = new WebAnalysis(cfg);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            var typeAnalysis = new TypeInferenceAnalysis(cfg, methodDefinition.ReturnType);
            typeAnalysis.Analyze();

            methodBody.UpdateVariables();

            return methodBody;
        }
    }
}
