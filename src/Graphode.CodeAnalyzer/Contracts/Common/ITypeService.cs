using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Contracts.Common
{
    public interface ITypeService : ICleanableIndex
    {
        void Load(List<ModuleDefinition> modules);
        List<TypeReference> GetTypesImplementing(string interfaceKey);
    }
}
