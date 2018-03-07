using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Graph
{
    public class MethodToMethodRelationship
    {
        public PublicMethodNode Caller { get; set; }
        public PublicMethodNode Callee { get; set; }
    }
}
