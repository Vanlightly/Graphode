using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public class Triple
    {
        public TripleObject From { get; set; }
        public TripleObject To { get; set; }

        public string ParentAssembly { get; set; }
        public MethodDefinition ParentMethod { get; set; }
        public bool SetsObjectInitializerMember { get; set; }
        public string ConstructorInstructionKey { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var triple = obj as Triple;
            if (triple == null)
                return false;

            if (triple.From.ObjectKey == null || From.ObjectKey == null)
            {
                if (triple.From.InstructionKey != null || From.InstructionKey != null)
                    return false;

                return triple.To.InstructionKey.Equals(To.InstructionKey)
                    && triple.To.ObjectKey.Equals(To.ObjectKey);
            }
            else
            {
                return triple.From.InstructionKey.Equals(From.InstructionKey)
                    && triple.From.ObjectKey.Equals(From.ObjectKey)
                    && triple.To.InstructionKey.Equals(To.InstructionKey)
                    && triple.To.ObjectKey.Equals(To.ObjectKey);
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = 0;

                if (From.ObjectKey != null)
                    result = (result * 397) ^ From.ObjectKey.GetHashCode();

                if (From.InstructionKey != null)
                    result = (result * 397) ^ From.InstructionKey.GetHashCode();

                result = (result * 397) ^ To.ObjectKey.GetHashCode();
                result = (result * 397) ^ To.InstructionKey.GetHashCode();

                return result;
            }
        }

        public override string ToString()
        {
            return From.ToString() + " -> " + To.ToString();
        }

        //public string ToLinkString()
        //{
        //    return From.ToInstructionKeyString() + " -> " + To.ToInstructionKeyString();
        //}
    }
}
