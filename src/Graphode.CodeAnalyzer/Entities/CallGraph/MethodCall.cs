using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.CallGraph
{
    public class MethodCall
    {
        public MethodReference MethodCalled { get; set; }
        public Instruction Instruction { get; set; }
        public MethodDefinition OwnerMethod { get; set; }
    }
}
