using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;

namespace Graphode.CodeAnalyzer.Contracts.AssignmentGraph
{
    public interface IAssignmentGraphWalker
    {
        IList<Triple> GetDirectAssignmentsFrom(HashSet<string> targetObjectKeys);
        IList<Triple> GetDirectAssignmentsTo(HashSet<string> targetObjectKeys);

        IList<AssignmentTree> PerformBacktrackingSearch(string startingObjectKey,
            ISourceDetector targetMatcher,
            List<GoToInstancePattern> permittedToGoInstancePatterns,
            List<string> searchBaseConstructorPatterns);

        IList<AssignmentTree> PerformBacktrackingSearch(Instruction instruction, MethodDefinition methodDefinition,
            ISourceDetector targetMatcher,
            List<GoToInstancePattern> permittedToGoInstancePatterns,
            List<string> searchBaseConstructorPatterns);
    }
}
