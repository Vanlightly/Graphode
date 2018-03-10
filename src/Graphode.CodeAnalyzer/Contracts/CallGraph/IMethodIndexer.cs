using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.Common;
using Graphode.CodeAnalyzer.Entities.CallGraph;

namespace Graphode.CodeAnalyzer.Contracts.CallGraph
{
    public interface IMethodIndexer : ICleanableIndex
    {

        void Load(ModuleDefinition module);
        void BuildMethodObjects(string appDomain);

        bool HasMethod(string signatureKey);
        List<MethodObject> GetMethods(string signatureKey);
        List<MethodObject> GetAllMethods();
    }
}
