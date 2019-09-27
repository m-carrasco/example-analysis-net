using Backend.Analyses;
using Backend.Model;
using Console.Utils;
using Model;
using Model.ThreeAddressCode.Instructions;
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

        [Test]
        public void Test2()
        {
            string sourceCode = @"
                using System;

                public class C {

                    public int Test(int userInput, int nonUserInput) {

                        userInput = IdentityTaintedHigh(userInput);
                        nonUserInput = Identity(nonUserInput);
        
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
                        
                        int high = IdentityTaintedLow(userInput);
                        int low = IdentityTaintedLow(nonUserInput);

                        return d * c * low;
                    }
    
                    public int IdentityTaintedHigh(int i) {
    	                return SourceStub.TaintHigh(i);
                    }

                    public int IdentityTaintedLow(int i) {
    	                return SourceStub.TaintLow(i);
                    }

                    public int Identity(int i){
    	                return i;
                    }
                }

                public class SourceStub{
                    public static int TaintHigh(int i) {return i;}
                    public static int TaintLow(int i) {return i;}
                }
            ";

            Compiler compiler = new Compiler();
            string compiledExamplePath = compiler.CompileSource(sourceCode);

            ControlFlowGraph cfg = null;
            MethodBody methodBody = Utils.GenerateTAC(compiledExamplePath, "Test", out cfg);

            Utils.SimplifyTAC(cfg, methodBody);

            string cfg_as_dot = Backend.Serialization.DOTSerializer.Serialize(cfg);

            TaintAnalysis taintAnalysis = new TaintAnalysis(cfg);
            var r = taintAnalysis.Analyze();

            var taintAtExit = r[cfg.Exit.Id].Output;

            var v0 = taintAtExit.Domain().Where(v => v.Name.Equals("userInput")).First();
            var v1 = taintAtExit.Domain().Where(v => v.Name.Equals("$r28")).First();
            var v2 = taintAtExit.Domain().Where(v => v.Name.Equals("$r34")).First();
            var v3 = taintAtExit.Domain().Where(v => v.Name.Equals("$r36")).First();
            var v4 = taintAtExit.Domain().Where(v => v.Name.Equals("local_0")).First();

            var v5 = taintAtExit.Domain().Where(v => v.Name.Equals("local_2")).First();

            Assert.AreEqual(taintAtExit.GetTaint(v0), TaintAnalysisStatus.HIGH);
            Assert.AreEqual(taintAtExit.GetTaint(v1), TaintAnalysisStatus.HIGH);
            Assert.AreEqual(taintAtExit.GetTaint(v2), TaintAnalysisStatus.HIGH);
            Assert.AreEqual(taintAtExit.GetTaint(v3), TaintAnalysisStatus.HIGH);
            Assert.AreEqual(taintAtExit.GetTaint(v4), TaintAnalysisStatus.HIGH);

            Assert.AreEqual(taintAtExit.GetTaint(v5), TaintAnalysisStatus.LOW);

            foreach (var v in taintAtExit.Domain().Where(v => v != v0 && v != v1 && v != v2 && v != v3 && v != v4 && v != v5))
                Assert.AreEqual(taintAtExit.GetTaint(v), TaintAnalysisStatus.NONE);
        }

        [Test]
        public void Test3()
        {
            string sourceCode = @"
                using System;

                public class C {

                    public static void Main(int userInput, int nonUserInput)
                    {
                        var r = Test(userInput,nonUserInput);
                        Console.WriteLine(r);
                    }
    
                    public static int Test(int userInput, int nonUserInput) {

                        userInput = IdentityTaintedHigh(userInput);
                        nonUserInput = Identity(nonUserInput);

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

                        int high = IdentityTaintedLow(userInput);
                        int low = IdentityTaintedLow(nonUserInput);

                        return d * c * low;
                    }

                    public static int IdentityTaintedHigh(int i) {
                        return SourceStub.TaintHigh(i);
                    }

                    public static int IdentityTaintedLow(int i) {
                        return SourceStub.TaintLow(i);
                    }

                    public static int Identity(int i){
                        return i;
                    }
                }

                public class SourceStub{
                    public static int TaintHigh(int i) {return i;}
                    public static int TaintLow(int i) {return i;}
                }
                ";

            Compiler compiler = new Compiler();
            string compiledExamplePath = compiler.CompileSource(sourceCode);

            ControlFlowGraph cfg = null;
            MethodBody methodBody = Utils.GenerateTAC(compiledExamplePath, "Main", out cfg);

            Utils.SimplifyTAC(cfg, methodBody);

            string cfg_as_dot = Backend.Serialization.DOTSerializer.Serialize(cfg);

            LeakChecker leakChecker = new LeakChecker(cfg);
            var r = leakChecker.Analyze();

            Assert.AreEqual(r.Count,1);
            Assert.AreEqual((r.First() as MethodCallInstruction).Method.Name, "WriteLine");
        }
    }
}
