using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses
{
    public interface IDatabaseFinder
    {
        string FindDatabaseName(TargetMethodMatch targetMethodMatch, MethodCall methodCall, MethodObject currentMethod, IAssignmentGraphWalker assignmentGraphWalker);
    }
}
