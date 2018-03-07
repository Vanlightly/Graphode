using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.CallGraph
{
    public class IndexedDelegate
    {
        public Instruction MethodAssignmentInstruction { get; set; }
        public MethodDefinition AssignedMethod { get; set; }
    }
}
