using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    public class DefaultSourceDetector : ISourceDetector
    {
        private HashSet<string> _matches;

        public DefaultSourceDetector(HashSet<string> matches)
        {
            _matches = matches;
        }

        public bool IsNameSource(Triple triple)
        {
            return _matches.Contains(triple.To.ObjectKey);
        }
    }
}
