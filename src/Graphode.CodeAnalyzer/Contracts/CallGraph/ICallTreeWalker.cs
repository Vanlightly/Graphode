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
        MethodGraph BuildCrossAssemblyGraph(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules);
        MethodGraph BuildPublicInnerAssemblyGraph(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules);
        MethodGraph BuildFullGraph(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules);
    }
}
