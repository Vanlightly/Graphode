using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public enum BacktrackResult
    {
        Success,
        BranchFailure,
        Stop
    }
}
