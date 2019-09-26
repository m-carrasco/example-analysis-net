using Backend.Model;
using Model.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewAnalyses.Interprocedueral
{
    class VariableRenamer
    {
        // que hacemos con los parametros?
        public void Rename(ControlFlowGraph cfg, int suffix)
        {
            var instructions = cfg.Nodes.SelectMany(i => i.Instructions);
            var variables = instructions.SelectMany(i => i.Variables);

            foreach (var v in variables)
                Rename(v, suffix);
        }

        public void Rename(IVariable v, int suffix)
        {
            if (v is LocalVariable local)
                local.Name = String.Format("{0}_{1}", v.Name, suffix);
            else
                throw new NotImplementedException();
        }
    }
}
