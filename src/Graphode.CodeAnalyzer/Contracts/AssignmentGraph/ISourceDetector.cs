using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;

namespace Graphode.CodeAnalyzer.Contracts.AssignmentGraph
{
    public interface ISourceDetector
    {
        bool IsNameSource(Triple triple);
    }
}
