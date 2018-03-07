using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;

namespace Graphode.CodeAnalyzer.Implementations.Resolvers
{
    public interface IDatabaseResolver
    {
        TargetMethodMatch IsTargetMethodMatch(MethodCall method, MethodObject currentMethod);
        ResourceKeyResult GetDatabaseKey(TargetMethodMatch targetMethodMatch, MethodCall method, MethodObject currentMethod);
    }
}
