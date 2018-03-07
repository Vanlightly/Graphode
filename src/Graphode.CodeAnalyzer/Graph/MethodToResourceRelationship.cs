using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Graph
{
    public class MethodToResourceRelationship
    {
        public PublicMethodNode Caller { get; set; }
        public ResourceAccessNode Resource { get; set; }
    }
}
