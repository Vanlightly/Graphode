using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public class ParseResult
    {
        public ParseResult()
        {
            Triples = new List<Triple>();
        }

        public Instruction LastInstructionParsed { get; set; }
        public List<Triple> Triples { get; set; }
    }
}
