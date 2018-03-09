using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Graph
{
    public class MethodToMethodRelationship
    {
        public MethodNode Caller { get; set; }
        public MethodNode Callee { get; set; }
    }
}
