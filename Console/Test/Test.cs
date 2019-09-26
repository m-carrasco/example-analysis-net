using Backend.Analyses;
using Backend.Model;
using Console.Utils;
using Model;
using Model.Types;
using NewAnalyses;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Utils
    {
        public static void SimplifyTAC(ControlFlowGraph cfg, MethodBody methodBody)
        {
            ForwardCopyPropagationAnalysis forwardCopyPropagationAnalysis = new ForwardCopyPropagationAnalysis(cfg);
            forwardCopyPropagationAnalysis.Analyze();
            forwardCopyPropagationAnalysis.Transform(methodBody);

            BackwardCopyPropagationAnalysis backwardCopyPropagationAnalysis = new BackwardCopyPropagationAnalysis(cfg);
            backwardCopyPropagationAnalysis.Analyze();
            backwardCopyPropagationAnalysis.Transform(methodBody);
        }

        public static MethodBody GenerateTAC(string path, string methodName, out ControlFlowGraph cfg)
        {
            Host host = new Host();
            ILoader provider = new CCIProvider.Loader(host);
            // load binaries that are gonna be analyzed
            var assembly = provider.LoadAssembly(path);

            // search for the method definition in the assembly
            // if you inspect this method definitions they are defined using the .NET IL instructions (aka bytecode)
            var allDefinedMethodsInAssembly = assembly.RootNamespace.Types // get all defined types in the assembly 
                .SelectMany(typeDefinition => typeDefinition.Methods); /// get all defined methods 

            var targetMethod = allDefinedMethodsInAssembly.Where(m => m.Name.Equals(methodName)).First();

            // transform it into a typed stackless three addres code representation
            // this is a result of analysis-net framework
            MethodBody methodBody = Transformations.ThreeAddressCode(targetMethod, out cfg);

            return methodBody;
        }
    }
    class Test
    {
        [Test]
        public void Test1()
        {
            string sourceCode = @"
                using System;

                public class C {
    
                    public int Test(int userInput, int nonUserInput) {
        
 		                userInput = SourceStub.TaintHigh(userInput); 
                        // ****** compute something depending on user input ******
                        int c = 0;
                        for (int i = 0; i < userInput; i++){
        	                c = userInput*2;
                        }
        
                        // ****** compute something not depending on user input ******
                        int d = 0;
                        for (int i = 0; i < nonUserInput; i++){
        	                d = nonUserInput*2;
                        }
        
                        return d * c;
                    }
                }

                public class SourceStub{
	                public static int TaintHigh(int i) {return i;}
                }
            ";

            Compiler compiler = new Compiler();
            string compiledExamplePath = compiler.CompileSource(sourceCode);

            ControlFlowGraph cfg = null;
            MethodBody methodBody = Utils.GenerateTAC(compiledExamplePath, "Test", out cfg);

            Utils.SimplifyTAC(cfg, methodBody);

            TaintAnalysis taintAnalysis = new TaintAnalysis(cfg);
            var r = taintAnalysis.Analyze();

            var taintAtExit = r[cfg.Exit.Id].Output;

            var v0 = taintAtExit.Domain().Where(v => v.Name.Equals("userInput")).First();
            var v1 = taintAtExit.Domain().Where(v => v.Name.Equals("$r24")).First();
            var v2 = taintAtExit.Domain().Where(v => v.Name.Equals("local_0")).First();
            Assert.AreEqual(taintAtExit.GetTaint(v0), TaintAnalysisStatus.HIGH);
            Assert.AreEqual(taintAtExit.GetTaint(v1), TaintAnalysisStatus.HIGH);
            Assert.AreEqual(taintAtExit.GetTaint(v2), TaintAnalysisStatus.HIGH);

            foreach (var v in taintAtExit.Domain().Where(v => v != v0 && v != v1 && v != v2))
                Assert.AreEqual(taintAtExit.GetTaint(v), TaintAnalysisStatus.NONE);
        }
    }
}
