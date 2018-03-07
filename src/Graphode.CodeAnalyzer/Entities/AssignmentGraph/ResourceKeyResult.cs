using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public enum AssignmentApproach
    {
        Unknown,
        BacktrackingSearch,
        MatchToApplication,
        MatchToAssembly,
        DiferentiateByNamingConvention,
        HardCoded
    }

    public class ResourceKeyResult
    {
        public ResourceKeyResult()
        {
            AccessMode = string.Empty;
        }

        public string Value { get; set; }
        public AssignmentApproach Approach { get; set; }
        public string AccessMode { get; set; }
    }
}
