using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Implementations
{
    public enum AnalysisPhaseResult
    {
        NoCompanyDllsFound,
        CouldNotFindBinFolder,
        Failed,
        Success
    }
}
