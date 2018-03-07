using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph.InstructionScopes
{
    public class MemberScope
    {
        public MemberScope()
        {
            Instructions = new List<Instruction>();
        }

        public ObjectInitializerScope Parent { get; set; }
        public List<Instruction> Instructions { get; set; }
        public ObjectInitializerScope NestedScope { get; set; }

        public bool HasNestedScope
        {
            get
            {
                return NestedScope != null;
            }
        }
    }

    public class ObjectInitializerScope
    {
        public ObjectInitializerScope()
        {
            MemberScopes = new List<MemberScope>();
        }

        public MethodDefinition ParentMethod { get; set; }
        public Instruction ConstructorInstruction { get; set; }
        public string ConstructorInstructionKey { get; set; }
        public string ConstructorOwnerKey { get; set; }
        public List<MemberScope> MemberScopes { get; set; }

        public int GetStartOffset()
        {
            return MemberScopes.Min(x => x.Instructions.Min(i => i.Offset));
        }

        public int GetEndOffset()
        {
            return MemberScopes.Max(x => x.Instructions.Max(i => i.Offset));
        }
    }
}
