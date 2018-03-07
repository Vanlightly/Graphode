using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph.InstructionScopes
{
    public class MainScope
    {
        public MainScope()
        {
            Instructions = new List<Instruction>();
            ObjectInitializerScopes = new List<ObjectInitializerScope>();
        }

        public List<Instruction> Instructions { get; set; }
        public List<ObjectInitializerScope> ObjectInitializerScopes { get; set; }
    }
}
