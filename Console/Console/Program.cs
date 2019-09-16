using Backend.Analyses;
using Backend.Model;
using Console.Utils;
using Model;
using Model.ThreeAddressCode.Values;
using Model.Types;
using NewAnalyses;
using System.Linq;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {
            RunLiveVariableAnalysisExample();
            RunZeroAnalysisExample();
        }

        public static void RunZeroAnalysisExample()
        {
            string liveVariableExample = CompileZeroAnalysisExample();
            Host host = new Host();
            ILoader provider = new CCIProvider.Loader(host);
            // load binaries that are gonna be analyzed
            var assembly = provider.LoadAssembly(liveVariableExample);
                
            // search for the method definition in the assembly
            // if you inspect this method definitions they are defined using the .NET IL instructions (aka bytecode)

            var allDefinedMethodsInAssembly = assembly.RootNamespace.Types // get all defined types in the assembly 
                .SelectMany(typeDefinition => typeDefinition.Methods); /// get all defined methods 

            var targetMethod = allDefinedMethodsInAssembly.Where(m => m.Name.Contains("TestZero")).First();

            // transform it into a typed stackless three addres code representation
            // this is a result of analysis-net framework
            ControlFlowGraph cfg = null;
            MethodBody methodBody = Transformations.ThreeAddressCode(targetMethod, out cfg);

            // on top of the three address representation we apply a live variable analysis
            // it is far easier to implement such analysis on this representation
            // if you intent to apply it on top of the IL bytecode, you will have to guess what you have in the stack for each instruction
            ZeroAnalysis zeroAnalysis = new ZeroAnalysis(cfg);
            var r = zeroAnalysis.Analyze();
        }

        public static string CompileZeroAnalysisExample()
        {
            string sourceCode = @"
            using System;
            public class C {
                public static void TestZero()
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
            }
            ";

            Compiler compiler = new Compiler();
            var binaryPath = compiler.CompileSource(sourceCode);

            return binaryPath;
        }

        public static void RunLiveVariableAnalysisExample()
        {
            string liveVariableExample = CompileLiveVariableAnalysisExample();
            Host host = new Host();
            ILoader provider = new CCIProvider.Loader(host);
            // load binaries that are gonna be analyzed
            var assembly = provider.LoadAssembly(liveVariableExample);

            // search for the method definition in the assembly
            // if you inspect this method definitions they are defined using the .NET IL instructions (aka bytecode)

            var allDefinedMethodsInAssembly = assembly.RootNamespace.Types // get all defined types in the assembly 
                .SelectMany(typeDefinition => typeDefinition.Methods); /// get all defined methods 

            var targetMethod = allDefinedMethodsInAssembly.Where(m => m.Name.Contains("Example")).First();

            // transform it into a typed stackless three addres code representation
            // this is a result of analysis-net framework
            ControlFlowGraph cfg = null;
            MethodBody methodBody = Transformations.ThreeAddressCode(targetMethod, out cfg);

            // on top of the three address representation we apply a live variable analysis
            // it is far easier to implement such analysis on this representation
            // if you intent to apply it on top of the IL bytecode, you will have to guess what you have in the stack for each instruction
            LiveVariablesAnalysis liveVariables = new LiveVariablesAnalysis(cfg);
            liveVariables.Analyze();

            // the result is the subset of live variables for each basic block in the cfg
            DataFlowAnalysisResult<Backend.Utils.Subset<IVariable>>[] result = liveVariables.Result;

            // export results
            // it generates two files in the same directory as the executable:
            // 1) cfg.dot contains a .dot representation of the control flow graph
            // 2) live_variable_analysis.txt plain text representation of the live variable analysis
            Export.ExportExample(cfg, result);
        }

        public static string CompileLiveVariableAnalysisExample()
        {
            string sourceCode = @"
                using System;
                public class C {
                    public int Example() {
                        int a = 3;
                        int b = 5;
                        int d = 4;
                        int x = 100;
                        int c = 0;
        
                        if (a > b){
        	                c = a + b;
                            d = 2;
                        }
        
                        c = 4;
        
                        return b * d + c;
                    }
                }
            ";

            Compiler compiler = new Compiler();
            var binaryPath = compiler.CompileSource(sourceCode);

            return binaryPath;
        }
    }
}
