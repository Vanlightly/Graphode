using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Graph;

namespace Graphode.CodeAnalyzer.Contracts.CallGraph
{
    public interface ICallTreeWalker
    {
        MethodGraph WalkMethods(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules);
    }
}
