using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses
{
    public interface IDatabaseAccessDetector
    {
        TargetMethodMatch IsDatabaseAccess(MethodCall method, MethodObject currentMethod);
    }
}
