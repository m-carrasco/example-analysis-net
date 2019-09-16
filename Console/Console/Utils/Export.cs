using Backend.Analyses;
using Backend.Model;
using Backend.Serialization;
using Model.ThreeAddressCode.Values;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Console.Utils
{
    class Export
    {
        public static void ExportExample(ControlFlowGraph cfg, DataFlowAnalysisResult<Backend.Utils.Subset<IVariable>>[] dataFlowAnalysisResults)
        {
            string cfg_as_dot = DOTSerializer.Serialize(cfg);

            // ideally we should have our method in DOTSerializer/DGMLSerializer and overload Serialize
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var node in cfg.Nodes)
            {
                if (node.Instructions.Count > 0)
                {
                    stringBuilder.AppendFormat("Live variables for control flow graph node with id {0} and first label {1} \n", node.Id, node.Instructions.First().Label);
                    stringBuilder.AppendLine(dataFlowAnalysisResults[node.Id].Input.ToString());
                }
            }

            WriteFile("live_variable_analysis.txt", stringBuilder.ToString());
            WriteFile("cfg.dot", cfg_as_dot);
        }

        public static void WriteFile(string filename, string content)
        {
            var filePath = System.IO.Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), filename);

            // Create the file.
            using (System.IO.FileStream fs = File.Create(filePath))
            {
                System.Console.WriteLine(filePath);
                Byte[] info = new UTF8Encoding(true).GetBytes(content);
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
    }
}
