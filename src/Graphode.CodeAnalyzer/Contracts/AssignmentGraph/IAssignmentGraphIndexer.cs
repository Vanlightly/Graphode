using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.Common;

namespace Graphode.CodeAnalyzer.Contracts.AssignmentGraph
{
    public interface IAssignmentGraphIndexer : ICleanableIndex
    {
        void IndexTriples(ModuleDefinition module);
    }
}
