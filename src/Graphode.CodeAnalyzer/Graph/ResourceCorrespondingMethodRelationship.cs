using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Graph
{
    public enum CorrespondingMethodDirection
    {
        NotDefined,
        Outgoing,
        Incoming
    }

    public class ResourceCorrespondingMethodRelationship
    {
        public ResourceAccessNode Resource { get; set; }
        public PublicMethodNode CorrespondingMethod { get; set; }
        public CorrespondingMethodDirection Direction { get; set; }

    }
}
