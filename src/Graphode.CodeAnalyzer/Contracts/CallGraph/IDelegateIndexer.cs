using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.Common;
using Graphode.CodeAnalyzer.Entities.CallGraph;

namespace Graphode.CodeAnalyzer.Contracts.CallGraph
{
    public interface IDelegateIndexer : ICleanableIndex
    {
        void Load(List<ModuleDefinition> modules);
        List<IndexedDelegate> GetAssignedMethods(MethodReference delegateInvocation);
    }
}
